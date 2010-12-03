using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using MuninNode;

namespace PluginASPNET {
	public class PluginASPNET : IPlugin {
		public const string name = "ASPNET";
		public const string version = "0.1";
		private IniParser config = SingletonConfig.Instance;
		private Dictionary<string, PerformanceCounter> perfcounters = new Dictionary<string, PerformanceCounter>();

		public void Load () {
			string Cat = config.GetOption("ASPNET", "category name","ASP.NET Applications");
			string Inst = "__Total__";
			try {
				PerformanceCounter aspnet_req_total = new PerformanceCounter();
				aspnet_req_total.CategoryName = Cat;
				aspnet_req_total.CounterName = "Requests Total";
				aspnet_req_total.InstanceName = Inst;
				aspnet_req_total.NextValue();
				perfcounters.Add("aspnet_req_total", aspnet_req_total);
			} catch (Exception) {
			}
			
			try {
				PerformanceCounter aspnet_req_failed = new PerformanceCounter();
				aspnet_req_failed.CategoryName = Cat;
				aspnet_req_failed.CounterName = "Requests Failed";
				aspnet_req_failed.InstanceName = Inst;
				aspnet_req_failed.NextValue();
				perfcounters.Add("aspnet_req_failed", aspnet_req_failed);
			} catch (Exception) {
			}

			try {
				PerformanceCounter aspnet_err_exec = new PerformanceCounter();
				aspnet_err_exec.CategoryName = Cat;
				aspnet_err_exec.CounterName = "Errors During Execution";
				aspnet_err_exec.InstanceName = Inst;
				aspnet_err_exec.NextValue();
				perfcounters.Add("aspnet_err_exec", aspnet_err_exec);
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
				sb.AppendFormat("graph_category {0}\n", "ASP.NET Applications");
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
