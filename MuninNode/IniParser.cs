using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class Section {
	private string name = "";
	public string Name {
		get {
			return name;
		}
	}
	private Dictionary<string, string> options = new Dictionary<string, string>();
	public Section(string name) {
		this.name = name;
	}
	public void Add (string name, string value) {
		if (options.ContainsKey(name)) {
			options[name] = value;
		} else {
			options.Add(name, value);
		}
	}
	
	public string GetOption (string optionname) {
		return GetOption(optionname, null);
	}
	
	public string GetOption (string optionname, string defval) {
		if (options.ContainsKey(optionname)) {
			return options[optionname];
		} else {
			return defval;
		}
	}
	
	public override string ToString () {
		StringBuilder sb = new StringBuilder();
		
		sb.AppendLine("[" + name + "]");
		foreach (KeyValuePair<string, string> option in options) {
			sb.AppendLine(option.Key + "=" + option.Value.Replace(";", "\\;"));
		}
		
		return sb.ToString();
	}
}

public class IniParser {
	private Dictionary<string, Section> SectionList = new Dictionary<string, Section>();
	private char[] commentSeparator = { ';' };
	private char[] optionSeparator = { '=', ':' };
	/// <summary>
	/// Generate a new ini file parser
	/// </summary>
	public IniParser() {
	}

	/// <summary>
	/// New ini file parser, takes a filename as argument and parse it on load
	/// </summary>
	/// <param name="filename">
	/// A <see cref="System.String"/>
	/// </param>
	public IniParser(string filename) {
		parse(filename);
	}
	
	/// <summary>
	/// Parse a file
	/// </summary>
	/// <param name="filename">
	/// A <see cref="System.String"/>
	/// </param>
	public bool parse (string filename) {
		try {
			parse(new StreamReader(filename));
			return true;
		} catch (FileNotFoundException) {
			return false;
		}
	}
	
	/// <summary>
	/// Parse a streamreader
	/// </summary>
	/// <param name="stream">
	/// A <see cref="StreamReader"/>
	/// </param>
	public void parse (StreamReader stream) {
		string sectionName = "DEFAULT";
		Section currentSection = null;
		while (!stream.EndOfStream) {
			string line = stream.ReadLine().Trim();
			if (line.StartsWith(";") || line == "") {
				continue;
			}
			if (line.StartsWith("[")) {
				sectionName = line.Substring(1, line.IndexOf("]") - 1).Trim();
				if (SectionList.ContainsKey(sectionName)) {
					currentSection = GetSection(sectionName);
				} else {
					currentSection = new Section(sectionName);
					SectionList.Add(sectionName, currentSection);
				}
			} else {
				string[] splitted_line = line.Split(commentSeparator);
				string optionline = "";
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < splitted_line.Length; i++) {
					sb.Append(splitted_line[i]);
					if (!splitted_line[i].EndsWith("\\")) {
						break;
					} else {
						sb.Append(";");
					}
				
				}
				optionline = sb.ToString().Replace("\\;", ";");
				string[] opt = optionline.Split(optionSeparator, 2);
				if (currentSection == null) {
					currentSection = new Section(sectionName);
					SectionList.Add(sectionName, currentSection);
				}
				currentSection.Add(opt[0].Trim(), opt[1].Trim());
			}
		}
	}
	
	/// <summary>
	/// Returns a text representation of the object.
	/// Can be used to save as ini file
	/// </summary>
	/// <returns>
	/// A <see cref="System.String"/>
	/// </returns>
	public override string ToString () {
		StringBuilder sb = new StringBuilder();
		if (SectionList.ContainsKey("DEFAULT")) {
			sb.AppendLine(GetSection("DEFAULT").ToString());
		}
		foreach (Section section in SectionList.Values) {
			if (section.Name == "DEFAULT") {
				continue;
			}
			sb.AppendLine(section.ToString());
		}
		return sb.ToString();
	}

	/// <summary>
	/// Check if the objects contains a section with the given name
	/// </summary>
	/// <param name="name">
	/// A <see cref="System.String"/>
	/// </param>
	/// <returns>
	/// A <see cref="System.Boolean"/>
	/// </returns>
	public bool HasSection (string name) {
		return SectionList.ContainsKey(name);
	}

	/// <summary>
	/// Returns the given section if exists, null otherwise.
	/// </summary>
	/// <param name="name">
	/// A <see cref="System.String"/>
	/// </param>
	/// <returns>
	/// A <see cref="Section"/>
	/// </returns>
	public Section GetSection (string name) {
		if (SectionList.ContainsKey(name)) {
			return SectionList[name];
		} else {
			return null;
		}
	}
	
	/// <summary>
	/// Get the option value for the given section name and option name
	/// return null if neither section hasn't been found nor option
	/// </summary>
	/// <param name="sectionname">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="optionname">
	/// A <see cref="System.String"/>
	/// </param>
	/// <returns>
	/// A <see cref="System.String"/>
	/// </returns>
	public string GetOption (string sectionname, string optionname) {
		return GetOption(sectionname, optionname, null);
	}

	/// <summary>
	/// Get the option value for the given section name and option name
	/// return the default value if neither section hasn't been found nor option
	/// </summary>
	/// <param name="sectionname">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="optionname">
	/// A <see cref="System.String"/>
	/// </param>
	/// <returns>
	/// A <see cref="System.String"/>
	/// </returns>
	public string GetOption (string sectionname, string optionname, string defval) {
		if (SectionList.ContainsKey(sectionname)) {
			return SectionList[sectionname].GetOption(optionname, defval);
		} else {
			return defval;
		}
	}
}
