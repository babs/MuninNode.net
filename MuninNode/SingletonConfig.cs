using System;

// Using http://csharpindepth.com/Articles/General/Singleton.aspx model
namespace MuninNode {
	public sealed class SingletonConfig {
		static readonly IniParser instance = new IniParser();
		SingletonConfig() {
		}
		static SingletonConfig() {
		}
		public static IniParser Instance {
			get {
				return instance;
			}
		}
	}
}

