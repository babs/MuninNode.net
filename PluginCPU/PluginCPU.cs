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
		private Dictionary<string, PerformanceCounter> perfcounters = new Dictionary<string, PerformanceCounter>();
		private Dictionary<string,float?[]> cycliclists = new Dictionary<string, float?[]>();
		private Thread updater = null;
		private EventWaitHandle waiter = new EventWaitHandle(false, EventResetMode.ManualReset);
		private int indice = 0;
		private int lenretention = 300;
		
		public void Load () {
			string Cat = "Processor";
			string Inst = "_Total";
			try {
				PerformanceCounter cpu_dpc_time = new PerformanceCounter();
				cpu_dpc_time.CategoryName = Cat;
				cpu_dpc_time.CounterName = "% DPC Time";
				cpu_dpc_time.InstanceName = Inst;
				cpu_dpc_time.NextValue();
				perfcounters.Add("cpu_dpc_time", cpu_dpc_time);
				cycliclists.Add("cpu_dpc_time", new float?[lenretention]);
			} catch (Exception) {
			}
			
			try {
				PerformanceCounter cpu_idle_time = new PerformanceCounter();
				cpu_idle_time.CategoryName = Cat;
				cpu_idle_time.CounterName = "% Idle Time";
				cpu_idle_time.InstanceName = Inst;
				cpu_idle_time.NextValue();
				perfcounters.Add("cpu_idle_time", cpu_idle_time);
				cycliclists.Add("cpu_idle_time", new float?[lenretention]);
			} catch (Exception) {
			}
			
			try {
				PerformanceCounter cpu_interrupt_time = new PerformanceCounter();
				cpu_interrupt_time.CategoryName = Cat;
				cpu_interrupt_time.CounterName = "% Interrupt Time";
				cpu_interrupt_time.InstanceName = Inst;
				cpu_interrupt_time.NextValue();
				perfcounters.Add("cpu_interrupt_time", cpu_interrupt_time);
				cycliclists.Add("cpu_interrupt_time", new float?[lenretention]);
			} catch (Exception) {
			}
			
			try {
				PerformanceCounter cpu_privileged_time = new PerformanceCounter();
				cpu_privileged_time.CategoryName = Cat;
				cpu_privileged_time.CounterName = "% Privileged Time";
				cpu_privileged_time.InstanceName = Inst;
				cpu_privileged_time.NextValue();
				perfcounters.Add("cpu_privileged_time", cpu_privileged_time);
				cycliclists.Add("cpu_privileged_time", new float?[lenretention]);
			} catch (Exception) {
			}
			
			try {
				PerformanceCounter cpu_processor_time = new PerformanceCounter();
				cpu_processor_time.CategoryName = Cat;
				cpu_processor_time.CounterName = "% Processor Time";
				cpu_processor_time.InstanceName = Inst;
				cpu_processor_time.NextValue();
				perfcounters.Add("cpu_processor_time", cpu_processor_time);
				cycliclists.Add("cpu_processor_time", new float?[lenretention]);
			} catch (Exception) {
			}
			
			try {
				PerformanceCounter cpu_user_time = new PerformanceCounter();
				cpu_user_time.CategoryName = Cat;
				cpu_user_time.CounterName = "% User Time";
				cpu_user_time.InstanceName = Inst;
				cpu_user_time.NextValue();
				perfcounters.Add("cpu_user_time", cpu_user_time);
				cycliclists.Add("cpu_user_time", new float?[lenretention]);
			} catch (Exception) {
			}
			
			updater = new Thread(UpdateCounters);
			updater.Start();
			
			Console.WriteLine(name + " Loaded");
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
			Console.WriteLine(name + " Unloaded");
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
