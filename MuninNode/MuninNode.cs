using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Configuration;
using System.Configuration.Install;
using System.ComponentModel;
using System.ServiceProcess;
using ddegois.Net.CIDR;

namespace MuninNode {
	public class MuninNode : ServiceBase {
		public const string version = "0.1b";
		private Thread mainthread = null;
		private static IniParser config = SingletonConfig.Instance;
		public static string nodename;
		private List<IPlugin> plugins = new List<IPlugin>();
		private static Dictionary<string, IPlugin> handlers = new Dictionary<string, IPlugin>();
		// Pre generated list of handlers
		public static string handlerList = null;
		// Set as public to allow threds to unregister themselves
		public static List<Thread> threadList = new List<Thread>();
		private Logger logger = Logger.Instance;
		
		// Horible cludge to unlock blocking call of AcceptTcpClient();
		private int listeningPort = 0;
		private IPAddress listeningIp = null;


		void Run () {
			// Load configuration from file
			config.parse(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "MuninNode.ini"));
			
			string[] includes = config.GetOptions("includes");
			if (includes != null) {
				foreach (string inc in includes) {
					config.parse(inc);
				}
			}

			#region Load plugins
			string folder = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Plugins");
			if (Directory.Exists(folder)) {
				string[] files = Directory.GetFiles(folder, "*.dll");
				foreach (string file in files) {
					Assembly assembly = Assembly.LoadFile(file);
					Type[] types = assembly.GetTypes();
					foreach (Type type in types) {
						try {
							IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
							plugin.Load();
							string[] pluginhandlers = plugin.AutoConfig();
							if (pluginhandlers.Length > 0) {
								int registered_handlers = 0;
								for (int i = 0; i < pluginhandlers.Length; i++) {
									if (handlers.ContainsKey(pluginhandlers[i])) {
										logger.Log(
											"WARNING: Plugin {0} is trying to register {1} handler but {2} already registered it.",
											plugin.GetName(),
											pluginhandlers[i],
											handlers[pluginhandlers[i]].GetName()
											);
									} else {
										registered_handlers++;
										handlers.Add(pluginhandlers[i], plugin);
									}
								}
								if (registered_handlers > 0) {
									plugins.Add(plugin);
								} else {
									plugin.UnLoad();
								}
							} else {
								plugin.UnLoad();
							}
						} catch (Exception e) {
							logger.Log("Error while loading {0}: {1}", file, e.ToString());
						}
					}
				}
				StringBuilder sb = new StringBuilder();
				bool first = true;
				foreach (string k in handlers.Keys) {
					if (first) {
						first = false;
					} else {
						sb.Append(" ");
					}
					sb.Append(k);
				}
				handlerList = sb.ToString();
			}
			#endregion
			
			foreach (string h in handlers.Keys) {
				logger.Log("{0} is handled by {1} version: {2}", h, handlers[h].GetName(), handlers[h].GetVersion());
			}
			
			
			listeningPort = Convert.ToInt32(config.GetOption("GENERAL", "port", "4949"));
			listeningIp = IPAddress.Parse(config.GetOption("GENERAL", "listen on", "0"));
			TcpListener listener = new TcpListener(listeningIp, listeningPort);
			try {
				listener.Start();
			} catch (SocketException e) {
				logger.Log(e.Message);
				logger.Log("Process STOPED");
				return;
			}

			#region Build allowed host CIDRCollection
			CIDRCollection allowed_hosts = new CIDRCollection();
			foreach (string ip in config.GetOption("GENERAL", "allow", "127.0.0.1").Split(',')) {
				allowed_hosts.Add(ip.Trim());
			}
			#endregion
			
			Int32 inactivity_timeout = Convert.ToInt32(config.GetOption("GENERAL", "inacivity timeout", "10"));
			
			nodename = config.GetOption("GENERAL", "hostname", Environment.MachineName);
			try {
				while (true) {
					TcpClient client = listener.AcceptTcpClient();
					client.ReceiveTimeout = 1000 * inactivity_timeout;

					if (!allowed_hosts.Has(((IPEndPoint)client.Client.RemoteEndPoint).Address)) {
						logger.Log("Connection from {0} not allowed. rejected.", ((IPEndPoint)client.Client.RemoteEndPoint).Address);
						client.Close();
						continue;
					}
					
					ClientHandler ch = new ClientHandler(client);
					
					Thread clientThread = new Thread(ch.Run);
					ch.mythread = clientThread;
					lock (threadList) {
						threadList.Add(clientThread);
					}
					clientThread.Start();
				}

			} catch (ThreadAbortException) {
				foreach (IPlugin plugin in plugins) {
					plugin.UnLoad();
				}
				foreach (Thread t in threadList) {
					logger.Log("Abort thtread");
					t.Abort();
				}
			}
		}
		
		public static string GetOption (string sectionname, string optionname, string defval) {
			return config.GetOption(sectionname, optionname, defval);
		}

		public static IPlugin GetHandler (string handler) {
			if (handlers.ContainsKey(handler)) {
				return handlers[handler];
			} else {
				return null;
			}
		}

		public static bool HasHandler (string handler) {
			return handlers.ContainsKey(handler);
		}

		#region service handling and main emulation by calling run
		
		protected override void OnStart (string[] args) {
			OnStart();
		}

		protected void OnStart () {
			if (mainthread == null) {
				mainthread = new Thread(Run);
				mainthread.Start();
			}
		}
		protected override void OnStop () {
			if (mainthread != null) {
				mainthread.Abort();
				try {
					TcpClient tc = new TcpClient();
					if (config.GetOption("GENERAL", "listen on", "0") != "0") {
						tc.Connect(listeningIp, listeningPort);
					} else {
						tc.Connect("127.0.0.1", listeningPort);
					}
				} catch (Exception) {}
				mainthread = null;
			}
		}

		public static void Main (string[] args) {
			if (Environment.UserInteractive || Environment.OSVersion.Platform == PlatformID.Unix) {
				Logger.Instance.EnableConsole();
				MuninNode t = new MuninNode();
				t.OnStart();
				Console.WriteLine("Run started. press return to stop.");
				Console.ReadLine();
				t.OnStop();
			} else {
				Logger.Instance.EnableEventlog();
				ServiceBase.Run(new MuninNode());
			}
		}
		#endregion
	}
}

#region Service installation
[RunInstallerAttribute(true)]
public class MuninNodeInstaller : Installer {
	public MuninNodeInstaller() : base() {
		ServiceInstaller TSSI = new ServiceInstaller();
		TSSI.ServiceName = "MuninNode";
		TSSI.DisplayName = "MuninNode";
		TSSI.StartType = ServiceStartMode.Automatic;
		this.Installers.Add(TSSI);
		
		ServiceProcessInstaller TSPI = new ServiceProcessInstaller();
		TSPI.Account = ServiceAccount.LocalSystem;
		this.Installers.Add(TSPI);
	}
}
#endregion
