using System;
using System.Reflection;
using System.Diagnostics;

namespace MuninNode {
	public sealed class Logger {
		static readonly Logger instance = new Logger();
		private bool logToConsole = false;
		private bool logToEventLog = false;
		private EventLog eventlog = null;

		Logger() {
		}

		public static Logger Instance {
			get {
				return instance;
			}
		}

		public void Log (string message) {
			string caller = Assembly.GetCallingAssembly().GetName().Name;
			if (logToEventLog) {
				if (eventlog == null) {
					eventlog = new EventLog();
					eventlog.Source = "MuninNode";
				}
				eventlog.WriteEntry(caller + ": " + message);
			}
			if (logToConsole) {
				Console.WriteLine(DateTime.Now + " " + caller + ": " + message);
			}
		}

		public void Log (string format, Object o1) {
			Log(String.Format(format, o1));
		}

		public void Log (string format, Object o1, Object o2) {
			Log(String.Format(format, o1, o2));
		}

		public void Log (string format, Object o1, Object o2, Object o3) {
			Log(String.Format(format, o1, o2, o3));
		}

		public void Log (string format, Object[] os) {
			Log(String.Format(format, os));
		}

		public void EnableConsole () {
			logToConsole = true;
		}

		public void EnableEventlog () {
			logToEventLog = true;
		}
		
	}
}
