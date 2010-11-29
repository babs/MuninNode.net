using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace MuninNode {
	public class ClientHandler {
		private TcpClient client = null;
		private char[] argSeparator = new char[] { ' ' };
		public Thread mythread = null;
		public ClientHandler(TcpClient client) {
			this.client = client;
		}
		public void Run () {
			Console.WriteLine(client.Client.RemoteEndPoint.ToString());
			if (client.Connected) {
				string remoteaddr = client.Client.RemoteEndPoint.ToString();
				Console.WriteLine("Got a client: {0}", remoteaddr);
				StreamWriter sw = new StreamWriter(client.GetStream());
				StreamReader sr = new StreamReader(client.GetStream());
				try {
					sw.Write("# munin node at {0} \n", MuninNode.nodename);
					sw.Flush();
					while (client.Connected && !sr.EndOfStream) {
						string[] spmsg = sr.ReadLine().Split(argSeparator, 2);
						if (spmsg[0] == "quit") {
							client.Close();
							break;
						} else if (spmsg[0] == "version") {
							sw.Write("MuninNode.net version:" + MuninNode.version);
						} else if (spmsg[0] == "nodes") {
							sw.Write(MuninNode.nodename + "\n");
							sw.Write(".\n");
						} else if (spmsg[0] == "list") {
							if (spmsg.Length == 1 || spmsg[1] == MuninNode.nodename) {
								sw.Write(MuninNode.handlerList);
							}
							sw.Write("\n");
						} else if (spmsg[0] == "config") {
							if (spmsg.Length < 2 || !MuninNode.handlers.ContainsKey(spmsg[1])) {
								sw.Write("# Unknown service\n.\n");
							} else {
								string conf = MuninNode.handlers[spmsg[1]].Config(spmsg[1]);
								sw.Write(conf);
								if (!conf.EndsWith("\n")) {
									sw.Write("\n.\n");
								} else {
									sw.Write(".\n");
								}
							}
						} else if (spmsg[0] == "fetch") {
							if (spmsg.Length < 2 || !MuninNode.handlers.ContainsKey(spmsg[1])) {
								sw.Write("# Unknown service\n.\n");
							} else {
								string conf = MuninNode.handlers[spmsg[1]].Fetch(spmsg[1]);
								sw.Write(conf);
								if (!conf.EndsWith("\n")) {
									sw.Write("\n.\n");
								} else {
									sw.Write(".\n");
								}
							}
						} else {
							sw.Write("# Unknown command. Try list, nodes, config, fetch, version or quit\n");
						}
						if (client.Connected) {
							sw.Flush();
						}
					}
				} catch (SystemException e) {
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
				}
				if ( client.Connected ) {
					client.Close();
				}
				Console.WriteLine("Client disconnected: {0}", remoteaddr);
				lock (MuninNode.threadList) {
					MuninNode.threadList.Remove(mythread);
				}
			}
		}
	}
}

