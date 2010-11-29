using System;
using System.Collections.Generic;
using System.Text;

namespace MuninNode {
    public interface IPlugin {
		/// <summary>
		/// Function called on plugin load (initialization) 
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		void Load();
		
		/// <summary>
		/// Function called on plugin UNload (destruction)
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		void UnLoad();
		
		/// <summary>
		/// Return the name of the plugin 
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
        string GetName();
		
		/// <summary>
		/// Return the version of the plugin 
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
        string GetVersion();
		
		/// <summary>
		/// Return the config for the given probe 
		/// </summary>
		/// <param name="probe">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
        string Config(string probe);
		
		/// <summary>
		/// Returns the value of the given probe 
		/// </summary>
		/// <param name="probe">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		string Fetch (string probe);
		
		/// <summary>
		/// Returns the list of probe handled by thins plugin
		/// Empty list will disable the plugin
		/// </summary>
		/// <returns>
		/// A <see cref="System.String[]"/>
		/// </returns>
	    string[] AutoConfig();
    }
}
