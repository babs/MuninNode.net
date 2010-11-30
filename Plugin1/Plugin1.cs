using System;
using MuninNode;

namespace Plugin1 {
	public class Plugin1 : IPlugin {
		public const string name = "Plugin1";
		public const string version = "0.1a";
		public void Load () {
			Console.WriteLine(name + " Loaded");
		}
		public void UnLoad () {
			Console.WriteLine(name + " Unloaded");
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
			if (MuninNode.MuninNode.GetOption("Plugin1", "load", "true") == "false") {
				return new string[] {  };
			} else {
				return new string[] { "test" };
			}
		}
	}
}
