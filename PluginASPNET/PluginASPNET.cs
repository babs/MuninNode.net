using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using MuninNode;

namespace PluginASPNET {
	public class PluginASPNET : IPlugin {
		public const string name = "ASPNET";
		public const string version = "0.2";
		private IniParser config = SingletonConfig.Instance;
		private Logger logger = Logger.Instance;
		private Dictionary<string, List<PerformanceCounter>> perfcounters = new Dictionary<string, List<PerformanceCounter>>();

		private bool RegisterPerfCounter(string RegistrationName, string CategoryName, string CounterName, string InstanceName) {
			try {
				PerformanceCounter pc = new PerformanceCounter();
				pc.CategoryName = CategoryName;
				pc.CounterName = CounterName;
				if (InstanceName != null) {
					pc.InstanceName = InstanceName;
				}
				pc.NextValue();
				if (!perfcounters.ContainsKey(RegistrationName)) {
					perfcounters.Add(RegistrationName, new List<PerformanceCounter>());
				}
				perfcounters[RegistrationName].Add(pc);
				return true;
			} catch (Exception) {
				return false;
			}
		}

		public void Load() {
			//string Cat = config.GetOption("ASPNET", "category name","ASP.NET Applications");
			string radix = "ASP.NET Apps ";
			string Inst = "__Total__";

			foreach (PerformanceCounterCategory category in PerformanceCounterCategory.GetCategories ()) {
				if (!category.CategoryName.StartsWith(radix)) {
					continue;
				}
				RegisterPerfCounter("aspnet_req_total",
			         category.CategoryName, "Requests Total", Inst);

				RegisterPerfCounter("aspnet_req_failed",
			         category.CategoryName, "Requests Failed", Inst);

				RegisterPerfCounter("aspnet_err_exec",
			         category.CategoryName, "Errors During Execution", Inst);
				logger.Log("regirstered " + category.CategoryName);
			}
			logger.Log("loaded");
		}

		public void UnLoad() {
			logger.Log("unloaded");
		}

		public string Fetch(string probe) {
			if (perfcounters.ContainsKey(probe)) {
				float sum = 0;
				foreach (PerformanceCounter pc in perfcounters[probe]) {
					sum += pc.NextValue();
				}
				return String.Format("{0}.value {1:0}\n", probe, sum);
			} else {
				return null;
			}
		}

		public string Config(string probe) {
			if (perfcounters.ContainsKey(probe)) {
				StringBuilder sb = new StringBuilder();
				PerformanceCounter pc = perfcounters[probe][0];
				string namewototal = pc.CounterName.Replace(" Total", "");
				sb.AppendFormat("graph_title {0}\n", namewototal);
				sb.Append("graph_args --base 1000 -l 0\n");
				sb.AppendFormat("graph_vlabel {0}/s\n", namewototal);
				sb.AppendFormat("graph_category {0}\n", "ASP.NET Applications");
				sb.AppendFormat("{0}.type DERIVE\n", probe);
				sb.AppendFormat("{0}.min 0\n", probe);
				sb.AppendFormat("{0}.label {1}\n", probe, namewototal);
				return sb.ToString();
			} else {
				return null;
			}
		}

		public string GetVersion() {
			return version;
		}

		public string GetName() {
			return name;
		}

		public string[] AutoConfig() {
			if (config.GetOption("ASPNET", "load", "true") == "false") {
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
