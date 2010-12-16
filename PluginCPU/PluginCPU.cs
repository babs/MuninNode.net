using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Threading;
using System.Globalization;
using MuninNode;

namespace PluginCPU {
	public class PluginCPU : IPlugin {
		public const string name = "CPU";
		public const string version = "0.1";
		private IniParser config = SingletonConfig.Instance;
		private Logger logger = Logger.Instance;
		private Dictionary<string, PerformanceCounter> perfcounters = new Dictionary<string, PerformanceCounter>();
		private Dictionary<string,float?[]> cycliclists = new Dictionary<string, float?[]>();
		private Thread updater = null;
		private EventWaitHandle waiter = new EventWaitHandle(false, EventResetMode.ManualReset);
		private int indice = 0;
		private int lenretention = 300;
		
		private bool RegisterPerfCounter(string RegistrationName, string CategoryName, string CounterName, string InstanceName) {
			try {
				PerformanceCounter pc = new PerformanceCounter();
				pc.CategoryName = CategoryName;
				pc.CounterName = CounterName;
				if (InstanceName != null) {
					pc.InstanceName = InstanceName;
				}
				pc.NextValue();
				perfcounters.Add(RegistrationName, pc);
				return true;
			} catch (Exception) {
				return false;
			}
		}

		public void Load () {
			string Cat = "Processor";
			string Inst = "_Total";

			if (RegisterPerfCounter( "cpu_dpc_time",
			                Cat, "% DPC Time", Inst )) {
				cycliclists.Add("cpu_dpc_time", new float?[lenretention]);
			}

			if (RegisterPerfCounter( "cpu_idle_time",
			                Cat, "% Idle Time", Inst )) {
				cycliclists.Add("cpu_idle_time", new float?[lenretention]);
			}

			if (RegisterPerfCounter( "cpu_interrupt_time",
			                Cat, "% Interrupt Time", Inst )) {
				cycliclists.Add("cpu_interrupt_time", new float?[lenretention]);
			}

			if (RegisterPerfCounter( "cpu_privileged_time",
			                Cat, "% Privileged Time", Inst )) {
				cycliclists.Add("cpu_privileged_time", new float?[lenretention]);
			}

			if (RegisterPerfCounter( "cpu_processor_time",
			                Cat, "% Processor Time", Inst )) {
				cycliclists.Add("cpu_processor_time", new float?[lenretention]);
			}

			if (RegisterPerfCounter( "cpu_user_time",
			                Cat, "% User Time", Inst )) {
				cycliclists.Add("cpu_user_time", new float?[lenretention]);
			}

			updater = new Thread(UpdateCounters);
			updater.Start();
			
			logger.Log("loaded");
		}
		
		private void UpdateCounters () {
			while (true) {
				if (indice >= lenretention) {
					indice = 0;
				}
				foreach (string cntname in cycliclists.Keys) {
					cycliclists[cntname][indice] = perfcounters[cntname].NextValue();
				}
				indice++;
				if (waiter.WaitOne(1000, false)) {
					break;
				}
			}
		}
		
		private float GetAverage (string countername) {
			if (cycliclists.ContainsKey(countername)) {
				int i = 0;
				float? sum = 0;
				foreach (float? val in cycliclists[countername]) {
					if (val != null) {
						sum += val;
						i++;
					}
				}
				if ( i != 0 ) {
					return (float) (sum/i);
				} else {
					return 0;
				}
			} else {
				return 0;
			}
		}
		
		public void UnLoad () {
			if (updater != null) {
				waiter.Set();
			}
			logger.Log("unloaded");
		}
		public string Fetch (string probe) {
			StringBuilder result = new StringBuilder();
			foreach (string c in cycliclists.Keys) {
				result.AppendFormat("{0}.value {1}\n", c, GetAverage(c).ToString("0.##",CultureInfo.InvariantCulture));
			}
			return result.ToString();
		}
		public string Config (string probe) {
			if (probe == "cpu") {
				StringBuilder sb = new StringBuilder();
				sb.Append("graph_title CPU Usage\n");
				sb.Append("graph_args --base 1000 -l 0\n");
				sb.Append("graph_vlabel % of cpu usage\n");
				sb.Append("graph_category system\n");
				bool first = true;
				foreach (string countername in new string[] { "cpu_processor_time", "cpu_interrupt_time", "cpu_dpc_time", "cpu_privileged_time", "cpu_user_time", "cpu_idle_time" }) {
					Console.WriteLine(countername);
					if (first) {
						sb.AppendFormat("{0}.draw AREA\n", countername);
					} else {
						sb.AppendFormat("{0}.draw STACK\n", countername);
					}
					sb.AppendFormat("{0}.type GAUGE\n", countername);
					sb.AppendFormat("{0}.label {1}\n", countername, perfcounters[countername].CounterName);
					first = false;
				}
				return sb.ToString();
			} else {
				return null;
			}
		}
		public string GetVersion () {
			return version;
		}
		public string GetName () {
			return name;
		}
		public string[] AutoConfig () {
			if (config.GetOption("CPU", "load", "true") == "false") {
				return new string[] {  };
			} else {
				if (perfcounters.Keys.Count > 0) {
					return new string[] { "cpu" };
				} else {
					return new string[] { };
				}
			}
		}
	}
}
