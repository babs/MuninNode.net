using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Threading;
using System.Globalization;
using MuninNode;

namespace PluginPhysicalDisk {
	public class PluginPhysicalDisk : IPlugin {
		public const string name = "PhysicalDisk";
		public const string version = "0.1";
		private IniParser config = SingletonConfig.Instance;
		private Logger logger = Logger.Instance;
		private Dictionary<string, PerformanceCounter> perfcounters = new Dictionary<string, PerformanceCounter>();
		private Dictionary<string,float?[]> cycliclists = new Dictionary<string, float?[]>();
		private Thread updater = null;
		private EventWaitHandle waiter = new EventWaitHandle(false, EventResetMode.ManualReset);
		private int indice = 0;
		private int lenretention = 300;
		private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static long GetCurrentUnixTimestampMillis() {
			return (long)(DateTime.UtcNow - Epoch).TotalMilliseconds;
		}

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

		public void Load() {
			string Cat = "PhysicalDisk";
			string Inst = "_Total";

			if (RegisterPerfCounter("disk_reads_per_sec",
			                        Cat, "Disk Reads/sec", Inst)) {
				cycliclists.Add("disk_reads_per_sec", new float?[lenretention]);
			}

			if (RegisterPerfCounter("disk_writes_per_sec",
			                        Cat, "Disk Writes/sec", Inst)) {
				cycliclists.Add("disk_writes_per_sec", new float?[lenretention]);
			}

			if (RegisterPerfCounter("disk_transfers_per_sec",
			                        Cat, "Disk Transfers/sec", Inst)) {
				cycliclists.Add("disk_transfers_per_sec", new float?[lenretention]);
			}

			if (RegisterPerfCounter("current_disk_queue_length",
			                        Cat, "Current Disk Queue Length", Inst)) {
				cycliclists.Add("current_disk_queue_length", new float?[lenretention]);
			}

			updater = new Thread(UpdateCounters);
			updater.Start();

			logger.Log("loaded");
		}

		private void UpdateCounters() {
			long begints = 0;
			while (true) {
				if (indice >= lenretention) {
					indice = 0;
				}
				begints = GetCurrentUnixTimestampMillis();
				foreach (string cntname in cycliclists.Keys) {
					cycliclists[cntname][indice] = perfcounters[cntname].NextValue();
				}
				indice++;
				long waittime = 1000 - (GetCurrentUnixTimestampMillis() - begints);
				if (waittime < 0) {
					waittime = 1;
				}
				if (waiter.WaitOne((int)waittime, false)) {
					break;
				}
			}
		}

		private float GetAverage(string countername) {
			if (cycliclists.ContainsKey(countername)) {
				int i = 0;
				float? sum = 0;
				foreach (float? val in cycliclists[countername]) {
					if (val != null) {
						sum += val;
						i++;
					}
				}
				if (i != 0) {
					return (float)(sum / i);
				} else {
					return 0;
				}
			} else {
				return 0;
			}
		}

		private float GetMax(string countername) {
			float max = 0;
			if (cycliclists.ContainsKey(countername)) {
				foreach (float? val in cycliclists[countername]) {
					if (val != null) {
						if (val > max) {
							max = (float)val;
						}
					}
				}
			}
			return max;
		}

		public void UnLoad() {
			if (updater != null) {
				waiter.Set();
			}
			logger.Log("unloaded");
		}

		public string Fetch(string probe) {
			StringBuilder result = new StringBuilder();
			foreach (string c in cycliclists.Keys) {
				result.AppendFormat("{0}.value {1}\n", c, GetAverage(c).ToString("0.##", CultureInfo.InvariantCulture));
				if (c == "disk_transfers_per_sec") {
					result.AppendFormat("{0}.value {1}\n", c + "_95p", Percentile(c, 95).ToString("0.##", CultureInfo.InvariantCulture));
					result.AppendFormat("{0}.value {1}\n", c + "_max", GetMax(c).ToString("0.##", CultureInfo.InvariantCulture));
				}
			}
			return result.ToString();
		}

		public string Config(string probe) {
			if (probe == "physicaldisk") {
				StringBuilder sb = new StringBuilder();
				sb.Append("graph_title Physical Disk\n");
				sb.Append("graph_args --base 1000 -l 0\n");
				sb.Append("graph_vlabel Physical Disk IO/s\n");
				sb.Append("graph_category disk\n");
				//bool first = true;
				foreach (string countername in new string[] { "disk_reads_per_sec", "disk_writes_per_sec", "disk_transfers_per_sec", "current_disk_queue_length" }) {
					/*-
					if (first) {
						sb.AppendFormat("{0}.draw AREA\n", countername);
					} else {
						sb.AppendFormat("{0}.draw STACK\n", countername);
					}
					*/
					sb.AppendFormat("{0}.type GAUGE\n", countername);
					sb.AppendFormat("{0}.label {1}\n", countername, perfcounters[countername].CounterName);
					if (countername == "disk_transfers_per_sec") {
						sb.AppendFormat("{0}.type GAUGE\n", countername + "_95p");
						sb.AppendFormat("{0}.label {1}\n", countername + "_95p", perfcounters[countername].CounterName + " (95p)");
						sb.AppendFormat("{0}.type GAUGE\n", countername + "_max");
						sb.AppendFormat("{0}.label {1}\n", countername + "_max", perfcounters[countername].CounterName + " (max)");
					}
					//first = false;
				}

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
			if (config.GetOption("PhysicalDisk", "load", "true") == "false") {
				return new string[] { };
			} else {
				if (perfcounters.Keys.Count > 0) {
					return new string[] { "physicaldisk" };
				} else {
					return new string[] { };
				}
			}
		}

		// From http://stackoverflow.com/questions/8137391/percentile-calculation
		public double Percentile(String countername, double excelPercentile) {
			excelPercentile /= 100;
			List<float> validvalues = new List<float>();
			if (cycliclists.ContainsKey(countername)) {
				foreach (float? val in cycliclists[countername]) {
					if (val != null) {
						validvalues.Add((float)val);
					}
				}
			}
			float[] sequence = validvalues.ToArray();
			Array.Sort(sequence);
			int N = sequence.Length;
			double n = (N - 1) * excelPercentile + 1;
			// Another method: double n = (N + 1) * excelPercentile;
			if (n == 1d) {
				return sequence[0];
			} else if (n == N) {
				return sequence[N - 1];
			} else {
				int k = (int)n;
				double d = n - k;
				return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
			}
		}

	}
}
