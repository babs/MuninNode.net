using System;
using MuninNode;

namespace Plugin1 {
	public class Plugin1 : IPlugin {
		public const string name = "Plugin1";
		public const string version = "0.1a";
		private IniParser config = SingletonConfig.Instance;
		private Logger logger = Logger.Instance;


		public void Load () {
			logger.Log("loaded");
		}
		public void UnLoad () {
			logger.Log("unloaded");
		}
		public string Fetch (string probe) {
			return "Fetch called into Plugin1 (" + probe + ")";
		}
		public string Config (string probe) {
			return "Config called into Plugin1 (" + probe + ")";
		}
		public string GetVersion () {
			return version;
		}
		public string GetName () {
			return name;
		}
		public string[] AutoConfig () {
			if (config.GetOption("Plugin1", "load", "false") == "false") {
				return new string[] {  };
			} else {
				return new string[] { "test" };
			}
		}
	}
}
