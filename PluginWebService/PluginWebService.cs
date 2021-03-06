using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using MuninNode;

namespace PluginWebService {
	public class PluginWebService : IPlugin {
		public const string name = "WebService";
		public const string version = "0.1";
		private IniParser config = SingletonConfig.Instance;
		private Logger logger = Logger.Instance;
		private Dictionary<string,PerformanceCounter> perfcounters = new Dictionary<string, PerformanceCounter>();

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
			string Cat = "Web Service";
			string Inst  = "_Total";
			
			RegisterPerfCounter("ws_current_conn",
			            Cat, "Current Connections", Inst);

			RegisterPerfCounter("ws_total_conn",
			            Cat, "Total Connection Attempts (all instances)", Inst);

			RegisterPerfCounter("ws_total_isapi",
			            Cat, "Total ISAPI Extension Requests", Inst);

			RegisterPerfCounter("ws_total_methods",
			            Cat, "Total Method Requests", Inst);

			logger.Log("loaded");
		}
		public void UnLoad () {
			logger.Log("unloaded");
		}
		public string Fetch (string probe) {
			if (perfcounters.ContainsKey(probe)) {
				return String.Format("{0}.value {1:0}\n",probe, perfcounters[probe].NextValue());
			} else {
				return null;
			}
		}
		public string Config (string probe) {
			if (perfcounters.ContainsKey(probe)) {
				StringBuilder sb = new StringBuilder();
				if (probe == "ws_current_conn") {
					PerformanceCounter pc = perfcounters[probe];
					sb.AppendFormat("graph_title {0}\n", pc.CounterName);
					sb.Append("graph_args --base 1000 -l 0\n");
					sb.AppendFormat("graph_vlabel {0}\n", pc.CounterName);
					sb.AppendFormat("graph_category {0}\n", pc.CategoryName);
					sb.AppendFormat("{0}.type GAUGE\n", probe);
					sb.AppendFormat("{0}.label {1}\n", probe, pc.CounterName);
				} else {
					PerformanceCounter pc = perfcounters[probe];
					string namewototal = pc.CounterName.Replace("Total ", "");
					sb.AppendFormat("graph_title {0}\n", namewototal);
					sb.Append("graph_args --base 1000 -l 0\n");
					sb.AppendFormat("graph_vlabel {0}/s\n", namewototal);
					sb.AppendFormat("graph_category {0}\n", pc.CategoryName);
					sb.AppendFormat("{0}.type DERIVE\n", probe);
					sb.AppendFormat("{0}.min 0\n", probe);
					sb.AppendFormat("{0}.label {1}\n", probe, namewototal);
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
			if (config.GetOption("WebService", "load", "true") == "false") {
				return new string[] {  };
			} else {
				List<string> working = new List<string>();
				foreach (string k in perfcounters.Keys) {
					working.Add(k);
				}
				return working.ToArray();
			}
		}
	}
}
