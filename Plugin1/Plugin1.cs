using System;
using System.Collections.Generic;
using System.Text;
using MuninNode;

namespace Plugin1 {
    public class Plugin1 : IPlugin {
		public const string name = "Plugin1";
		public const string version = "0.1a";
		public void Load() {
			Console.WriteLine("Plugin1 Loaded");
		}
		public void UnLoad() {
			Console.WriteLine("Plugin1 Unloaded");
		}
        public string Fetch(string probe) {
            return "Fetch called into Plugin1 (" + probe + ")";
        }
        public string Config (string probe) {
        	return "Config called into Plugin1 (" + probe + ")";
        }
        public string GetVersion() {
            return version;
        }
        public string GetName() {
            return name;
        }
        public string[] AutoConfig () {
        	string[] hanlders = new string[] { "test" };
        	return hanlders;
        }
    }
}
