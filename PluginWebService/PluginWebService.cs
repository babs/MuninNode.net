using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using MuninNode;

namespace PluginWebService {
	public class PluginWebService : IPlugin {
		public const string name = "WebService";
		public const string version = "0.1";
		private Dictionary<string,PerformanceCounter> perfcounters = new Dictionary<string, PerformanceCounter>();

		public void Load () {
			try {
				PerformanceCounter ws_total_conn = new PerformanceCounter();
				ws_total_conn.CategoryName = "Web Service";
				ws_total_conn.CounterName = "Total Connection Attempts (all instances)";
				ws_total_conn.InstanceName = "_Total";
				ws_total_conn.NextValue();
				perfcounters.Add("ws_total_conn", ws_total_conn);
			} catch (Exception) {
			}
			
			try {
				PerformanceCounter ws_total_isapi = new PerformanceCounter();
				ws_total_isapi.CategoryName = "Web Service";
				ws_total_isapi.CounterName = "Total ISAPI Extension Requests";
				ws_total_isapi.InstanceName = "_Total";
				ws_total_isapi.NextValue();
				perfcounters.Add("ws_total_isapi", ws_total_isapi);
			} catch (Exception) {
			}
			
			try {
				PerformanceCounter ws_total_methods = new PerformanceCounter();
				ws_total_methods.CategoryName = "Web Service";
				ws_total_methods.CounterName = "Total Method Requests";
				ws_total_methods.InstanceName = "_Total";
				ws_total_methods.NextValue();
				perfcounters.Add("ws_total_methods", ws_total_methods);
			} catch (Exception) {
			}
			
			Console.WriteLine(name+" Loaded");
		}
		public void UnLoad () {
			Console.WriteLine(name + " Unloaded");
		}
		public string Fetch (string probe) {
			if (perfcounters.ContainsKey(probe)) {
				return String.Format("{0}.value {1}\n",probe, perfcounters[probe].NextValue());
			} else {
				return null;
			}
		}
		public string Config (string probe) {
			if (perfcounters.ContainsKey(probe)) {
				PerformanceCounter pc = perfcounters[probe];
				string namewototal = pc.CounterName.Replace("Total ", "");
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat("graph_title {0}\n", namewototal);
				sb.Append("graph_args --base 1000 -l 0\n");
				sb.AppendFormat("graph_vlabel {0}/s\n", namewototal);
				sb.AppendFormat("graph_category {0}\n", pc.CategoryName);
				sb.AppendFormat("{0}.type DERIVE\n", probe);
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
			if (MuninNode.MuninNode.GetOption("WebService", "load", "true") == "false") {
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
