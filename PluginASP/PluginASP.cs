using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using MuninNode;

namespace PluginASP {
	public class PluginASP : IPlugin {
		public const string name = "ASP";
		public const string version = "0.1";
		private IniParser config = SingletonConfig.Instance;
		private Dictionary<string, PerformanceCounter> perfcounters = new Dictionary<string, PerformanceCounter>();

		public void Load () {
			string Cat = "Active Server Pages";
			try {
				PerformanceCounter asp_req_total = new PerformanceCounter();
				asp_req_total.CategoryName = Cat;
				asp_req_total.CounterName = "Requests Total";
				asp_req_total.NextValue();
				perfcounters.Add("asp_req_total", asp_req_total);
			} catch (Exception) {
			}
			
			try {
				PerformanceCounter asp_req_failed = new PerformanceCounter();
				asp_req_failed.CategoryName = Cat;
				asp_req_failed.CounterName = "Requests Failed Total";
				asp_req_failed.NextValue();
				perfcounters.Add("asp_req_failed", asp_req_failed);
			} catch (Exception) {
			}
			
			try {
				PerformanceCounter asp_err_exec = new PerformanceCounter();
				asp_err_exec.CategoryName = Cat;
				asp_err_exec.CounterName = "Errors During Script Runtime";
				asp_err_exec.NextValue();
				perfcounters.Add("asp_err_exec", asp_err_exec);
			} catch (Exception) {
			}
			
			Console.WriteLine(name + " Loaded");
		}
		public void UnLoad () {
			Console.WriteLine(name + " Unloaded");
		}
		public string Fetch (string probe) {
			if (perfcounters.ContainsKey(probe)) {
				return String.Format("{0}.value {1}\n", probe, perfcounters[probe].NextValue());
			} else {
				return null;
			}
		}
		public string Config (string probe) {
			if (perfcounters.ContainsKey(probe)) {
				StringBuilder sb = new StringBuilder();
				PerformanceCounter pc = perfcounters[probe];
				string namewototal = pc.CounterName.Replace(" Total", "");
				sb.AppendFormat("graph_title {0}\n", namewototal);
				sb.Append("graph_args --base 1000 -l 0\n");
				sb.AppendFormat("graph_vlabel {0}/s\n", namewototal);
				sb.AppendFormat("graph_category {0}\n", "Active Server Pages");
				sb.AppendFormat("{0}.type DERIVE\n", probe);
				sb.AppendFormat("{0}.min 0\n", probe);
				sb.AppendFormat("{0}.label {1}\n", probe, namewototal);
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
			if (config.GetOption("ASP", "load", "false") == "false") {
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
