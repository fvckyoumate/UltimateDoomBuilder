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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace CodeImp.DoomBuilder.Dehacked
{
	public class DehackedThing
	{
		#region ================== Variables

		private int number;
		private string name;
		private Dictionary<string, string> props;
		private int doomednum;
		private int initialframe;
		private string sprite;
		private int height;
		private int width;
		private List<string> bits;

		#endregion

		#region ================== Properties

		public int Number { get { return number; } }
		public Dictionary<string, string> Props { get { return props; } }
		public int DoomEdNum { get { return doomednum; } internal set { doomednum = value; } }
		public string Name { get { return name; } internal set { name = value; } }
		public int InitialFrame { get { return initialframe; } internal set { initialframe = value; } }
		public string Sprite { get { return sprite; } internal set { sprite = value; } }
		public int Height { get { return height; } internal set { height = value; } }
		public int Width { get { return width; } internal set { width = value; } }
		public List<string> Bits { get { return bits; } }

		#endregion

		#region ================== Constructor

		internal DehackedThing(int number, string name)
		{
			this.number = number;
			this.name = name;

			props = new Dictionary<string, string>();
			bits = new List<string>();
		}

		internal DehackedThing(int number, string name, Dictionary<string, string> props) : this(number, name)
		{
			foreach(string key in props.Keys)
			{
				this.props[key.ToLowerInvariant()] = props[key];
			}
		}

		#endregion

		internal void Process(Dictionary<int, DehackedFrame> frames, Dictionary<int, string> definedsprites, Dictionary<long, string> bitmnemonics, DehackedThing basething, HashSet<string> availablesprites)
		{
			if(basething != null)
			{
				doomednum = basething.DoomEdNum;
				foreach (string key in basething.Props.Keys)
					if (!props.ContainsKey(key))
						props[key] = basething.props[key];
			}

			foreach (KeyValuePair<string, string> kvp in props)
			{
				string prop = kvp.Key.ToLowerInvariant();
				string value = kvp.Value;

				switch (prop)
				{
					case "id #":
						int.TryParse(value, out doomednum);
						break;
					case "initial frame":
						if (int.TryParse(value, out initialframe))
						{
							if (frames.ContainsKey(initialframe))
							{
								// It doesn't seem to matter which rotation we select, UDB will automagically
								// find the correct sprites later
								string spritename = frames[initialframe].Sprite + Convert.ToChar(frames[initialframe].SpriteSubNumber + 'A');
								if (availablesprites.Contains(spritename + "0"))
									sprite = spritename + "0";
								else
									sprite = spritename + "1";
							}
						}
						break;
					case "width":
						if(int.TryParse(value, out width))
						{
							// Value is in 16.16 fixed point, so shift it
							width >>= 16;
						}
						break;
					case "height":
						if(int.TryParse(value, out height))
						{
							// Value is in 16.16 fixed point, so shift it
							height >>= 16;
						}
						break;
					case "bits":
						long allbits;
						if(long.TryParse(value, out allbits))
						{
							foreach (long mask in bitmnemonics.Keys)
								if ((mask & allbits) == mask)
									bits.Add(bitmnemonics[mask]);
						}
						else
						{
							foreach (string mnemonic in value.Split('+'))
								bits.Add(mnemonic.Trim().ToLowerInvariant());
						}
						break;
				}
			}
		}
	}
}
