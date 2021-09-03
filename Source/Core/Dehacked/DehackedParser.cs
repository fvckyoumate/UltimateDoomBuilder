#region ================== Copyright (c) 2021 Boris Iwanski

/*
 * This program is free software: you can redistribute it and/or modify
 *
 * it under the terms of the GNU General Public License as published by
 * 
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 *    but WITHOUT ANY WARRANTY; without even the implied warranty of
 * 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.If not, see<http://www.gnu.org/licenses/>.
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Data;

#endregion

namespace CodeImp.DoomBuilder.Dehacked
{
	internal sealed class DehackedParser
	{
		#region ================== Variables

		private StreamReader datareader;
		private List<DehackedThing> things;
		private string sourcename;
		private DataLocation datalocation;
		private int sourcelumpindex;
		private int linenumber;
		private Dictionary<int, DehackedFrame> frames;
		private Dictionary<string, string> texts;
		private Dictionary<int, string> sprites;

		private Dictionary<long, string> bitmnemonics = new Dictionary<long, string>()
		{
			{ 0x00000001, "special" },
			{ 0x00000002, "solid" },
			{ 0x00000004, "shootable" },
			{ 0x00000008, "nosector" },
			{ 0x00000010, "noblockmap" },
			{ 0x00000020, "ambush" },
			{ 0x00000040, "justhit" },
			{ 0x00000080, "justattacked" },
			{ 0x00000100, "spawnceiling" },
			{ 0x00000200, "nogravity" },
			{ 0x00000400, "dropoff" },
			{ 0x00000800, "pickup" },
			{ 0x00001000, "noclip" },
			{ 0x00002000, "slide" },
			{ 0x00004000, "float" },
			{ 0x00008000, "teleport" },
			{ 0x00010000, "missile" },
			{ 0x00020000, "dropped" },
			{ 0x00040000, "shadow" },
			{ 0x00080000, "noblood" },
			{ 0x00100000, "corpse" },
			{ 0x00200000, "infloat" },
			{ 0x00400000, "countkill" },
			{ 0x00800000, "countitem" },
			{ 0x01000000, "skullfly" },
			{ 0x02000000, "notdmatch" },
			{ 0x04000000, "translation" },
			{ 0x08000000, "unused1" },
			{ 0x10000000, "unused2" },
			{ 0x20000000, "unused3" },
			{ 0x40000000, "unused4" },
			{ 0x80000000, "translucent" },
		};

		#endregion

		#region ================== Enumerations

		private enum ParseSection
		{
			NONE,
			HEADER,
			THING,
			FRAME,
			WEAPON,
			CODEPTR,
			STRINGS,
			SPRITES,
			SOUNDS,
			TEXT
		}

		#endregion

		#region ================== Properties

		public List<DehackedThing> Things { get { return things; } }
		public Dictionary<string, string> Texts { get { return texts; } }

		#endregion

		#region ================== Constructor

		public DehackedParser()
		{
			things = new List<DehackedThing>();
			frames = new Dictionary<int, DehackedFrame>();
			texts = new Dictionary<string, string>();
			sprites = new Dictionary<int, string>();
		}

		#endregion

		#region ================== Parsing

		public bool Parse(TextResourceData data, DehackedData dehackeddata, HashSet<string> availablesprites)
		{
			ParseSection parsesection = ParseSection.HEADER;
			string line;
			string fieldkey = string.Empty;
			string fieldvalue = string.Empty;
			bool hasfieldvaluepair = false;
			int textreplaceoldcount = 0;
			int textreplacenewcount = 0;
			DehackedThing thing = null;
			DehackedFrame frame = null;

			sourcename = data.Filename;
			datalocation = data.SourceLocation;
			sourcelumpindex = data.LumpIndex;

			using (datareader = new StreamReader(data.Stream, Encoding.ASCII))
			{
				// Read header
				line = GetLine();
				if(line != "Patch File for DeHackEd v3.0")
				{
					LogError("Did not find expected Dehacked file header.");
					return false;
				}

				while (!datareader.EndOfStream)
				{
					line = GetLine();

					// Skip blank lines
					if (string.IsNullOrWhiteSpace(line))
					{
						if (parsesection == ParseSection.CODEPTR)
							parsesection = ParseSection.NONE;
						continue;
					}
					// Comment lines start with #. Apparently comments after payload are not supported
					else if (line.StartsWith("#"))
						continue;
					// field = values pairs
					else if (line.Contains("="))
					{
						string[] parts = line.Split('=');
						fieldkey = parts[0].Trim();
						fieldvalue = parts[1].Trim();
						hasfieldvaluepair = true;
					}
					else if (line.ToLowerInvariant().StartsWith("thing"))
					{
						ParseThing(line);
						continue; // Go to next line
					}
					else if (line.ToLowerInvariant().StartsWith("frame") && parsesection != ParseSection.CODEPTR)
					{
						Regex re = new Regex(@"frame\s+(\d+)", RegexOptions.IgnoreCase);
						Match m = re.Match(line);

						if (!m.Success)
						{
							LogError("Found frame definition, but frame header seems to be wrong.");
							return false;
						}

						int framenumber = int.Parse(m.Groups[1].Value);

						frame = new DehackedFrame(framenumber);
						frames[framenumber] = frame;

						parsesection = ParseSection.FRAME;
						continue; // Go to next line
					}
					else if (line.ToLowerInvariant().StartsWith("weapon"))
					{
						parsesection = ParseSection.WEAPON;
						continue; // Go to next line
					}
					else if (line.ToLowerInvariant().StartsWith("[codeptr]"))
					{
						parsesection = ParseSection.CODEPTR;
						continue; // Go to next line
					}
					else if (line.ToLowerInvariant().StartsWith("[strings]"))
					{
						parsesection = ParseSection.STRINGS;
						continue; // Go to next line
					}
					else if (line.ToLowerInvariant().StartsWith("[sprites]"))
					{
						parsesection = ParseSection.SPRITES;
						continue; // Go to next line
					}
					else if (line.ToLowerInvariant().StartsWith("[sounds]"))
					{
						parsesection = ParseSection.SOUNDS;
						continue; // Go to next line
					}
					else if (line.ToLowerInvariant().StartsWith("text"))
					{
						parsesection = ParseSection.TEXT;

						Regex re = new Regex(@"text\s+(\d+)\s+(\d+)", RegexOptions.IgnoreCase);
						Match m = re.Match(line);

						if (!m.Success)
						{
							LogError("Found text replacement definition, but text replacement header seems to be wrong.");
							return false;
						}

						textreplaceoldcount = int.Parse(m.Groups[1].Value);
						textreplacenewcount = int.Parse(m.Groups[2].Value);

						StringBuilder oldtext = new StringBuilder(textreplaceoldcount);
						while (textreplaceoldcount > 0)
						{
							int c = datareader.Read();

							// Ignore CR
							if (c == '\r') continue;

							oldtext.Append(Convert.ToChar(c));
							textreplaceoldcount--;
						}

						StringBuilder newtext = new StringBuilder();
						while (textreplacenewcount > 0)
						{
							int c = datareader.Read();

							// Ignore CR
							if (c == '\r') continue;

							if (c == '\n') linenumber++;

							newtext.Append(Convert.ToChar(c));
							textreplacenewcount--;
						}

						if (!datareader.EndOfStream && datareader.Read() != '\r' && datareader.Read() != '\n')
						{
							LogError("Expected CRLF after text replacement, got something else.");
							return false;
						}

						linenumber++;

						texts[oldtext.ToString()] = newtext.ToString();

						continue;
					}

					switch (parsesection)
					{
						case ParseSection.HEADER:
							if (hasfieldvaluepair)
							{
								if (fieldkey == "Doom version" && fieldvalue != "21")
									LogWarning("Unexpected Doom version. Expected 21, got " + fieldvalue + ". Parsing might not work correctly.");
								else if (fieldvalue == "Patch format" && fieldvalue != "6")
									LogWarning("Unexpected patch format. Expected 6, got " + fieldvalue + ". Parsing might not work correctly.");
							}
							break;
						case ParseSection.FRAME:
							if(hasfieldvaluepair)
							{
								frame.Props[fieldkey.ToLowerInvariant()] = fieldvalue;
							}
							break;
					}
				}
			}

			// Process text replacements
			foreach(int key in dehackeddata.Sprites.Keys)
			{
				string sprite = dehackeddata.Sprites[key];
				if (texts.ContainsKey(sprite))
					sprites[key] = texts[sprite];
				else
					sprites[key] = sprite;
			}

			// Process frames
			foreach(int key in dehackeddata.Frames.Keys)
			{
				if (!frames.ContainsKey(key))
					frames[key] = dehackeddata.Frames[key];
			}

			foreach(DehackedFrame f in frames.Values)
				f.Process(sprites, dehackeddata.Frames.ContainsKey(f.Number) ? dehackeddata.Frames[f.Number] : null);

			// Finalize things
			foreach (DehackedThing t in things)
				t.Process(frames, sprites, bitmnemonics, dehackeddata.Things.ContainsKey(t.Number) ? dehackeddata.Things[t.Number] : null, availablesprites);

			return true;
		}

		/// <summary>
		/// Returns a new line and increments the line number
		/// </summary>
		/// <returns>The read line</returns>
		private string GetLine()
		{
			linenumber++;
			return datareader.ReadLine().Trim();
		}

		/// <summary>
		/// Logs a warning with the given message.
		/// </summary>
		/// <param name="message">The warning message</param>
		private void LogWarning(string message)
		{
			string errsource = Path.Combine(datalocation.GetDisplayName(), sourcename);
			if (sourcelumpindex != -1) errsource += ":" + sourcelumpindex;

			message = "Dehacked warning in \"" + errsource + "\" line " + linenumber + ". " + message + ".";

			TextResourceErrorItem error = new TextResourceErrorItem(ErrorType.Warning, ScriptType.DEHACKED, datalocation, sourcename, sourcelumpindex, linenumber, message);

			General.ErrorLogger.Add(error);
		}

		/// <summary>
		/// Logs an error with the given message.
		/// </summary>
		/// <param name="message">The error message</param>
		private void LogError(string message)
		{
			string errsource = Path.Combine(datalocation.GetDisplayName(), sourcename);
			if (sourcelumpindex != -1) errsource += ":" + sourcelumpindex;

			message = "Dehacked error in \"" + errsource + "\" line " + linenumber + ". " + message + ".";

			TextResourceErrorItem error = new TextResourceErrorItem(ErrorType.Error, ScriptType.DEHACKED, datalocation, sourcename, sourcelumpindex, linenumber, message);

			General.ErrorLogger.Add(error);
		}

		private bool GetKeyValueFromLine(string line, out string key, out string value)
		{
			key = string.Empty;
			value = string.Empty;

			if (!line.Contains('='))
			{
				LogError("Line in thing definition didn't contain '='.");
				return false;
			}

			string[] parts = line.Split('=');
			key = parts[0].Trim().ToLowerInvariant();
			value = parts[1].Trim();

			return true;
		}

		private bool ParseThing(string line)
		{
			Regex re = new Regex(@"thing\s+(\d+)\s+\((.+)\)", RegexOptions.IgnoreCase);
			Match m = re.Match(line);

			if (!m.Success)
			{
				LogError("Found thing definition, but thing header seems to be wrong.");
				return false;
			}

			int dehthingnumber = int.Parse(m.Groups[1].Value);
			string dehthingname = m.Groups[2].Value;
			string fieldkey = string.Empty;
			string fieldvalue = string.Empty;

			DehackedThing thing = new DehackedThing(dehthingnumber, dehthingname);
			things.Add(thing);

			while(true)
			{
				line = GetLine();

				if (string.IsNullOrWhiteSpace(line)) break;
				if (line.StartsWith("#")) continue;

				if (!GetKeyValueFromLine(line, out fieldkey, out fieldvalue))
					return false;

				thing.Props[fieldkey] = fieldvalue;
			}

			return true;
		}

		#endregion


	}
}
