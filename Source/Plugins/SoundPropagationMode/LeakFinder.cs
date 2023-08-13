#region ================== Copyright (c) 2023 Boris Iwanski

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


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Map;

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	internal class LeakFinder
	{
		public SoundNode Start { get; }
		public SoundNode End { get; }
		public List<SoundNode> Nodes { get; }
		public HashSet<Sector> Sectors { get; }
		public bool Finished { get; internal set; }

		private ConcurrentDictionary<Linedef, SoundNode> linedefs2nodes;
		private int numblockingnodes;

		public LeakFinder(Sector source, Vector2D sourceposition, Sector destination, Vector2D destinationposition, HashSet<Sector> sectors)
		{
			if (!sectors.Contains(source) || !sectors.Contains(destination))
				throw new ArgumentException("Sound propagation domain does not contain both the start and end sectors");

			End = new SoundNode(destinationposition);
			Start = new SoundNode(sourceposition, End) { G = 0 };
			Sectors = sectors;

			Finished = false;

			Nodes = new List<SoundNode>() { Start, End };

			linedefs2nodes = new ConcurrentDictionary<Linedef, SoundNode>();

			GenerateNodes(sectors);

			PopulateStartEndNeighbors(source, Start);
			PopulateStartEndNeighbors(destination, End);
		}

		private bool CheckLinedefValidity(Linedef linedef)
		{
			if (linedef.Back == null)
				return false;

			if (linedef.Front.Sector == linedef.Back.Sector)
				return false;

			if (SoundPropagationDomain.IsSoundBlockedByHeight(linedef))
				return false;

			return Sectors.Contains(linedef.Front.Sector) && Sectors.Contains(linedef.Back.Sector);
		}

		private void GenerateNodes(HashSet<Sector> sectors)
		{
			foreach(Sector s in sectors)
			{
				IEnumerable<Sidedef> sidedefs = s.Sidedefs.Where(sd => CheckLinedefValidity(sd.Line));

				// Pass 1: create nodes
				foreach(Sidedef sd in sidedefs)
				{
					if(!linedefs2nodes.ContainsKey(sd.Line))
					{
						linedefs2nodes[sd.Line] = new SoundNode(sd.Line, End);
						Nodes.Add(linedefs2nodes[sd.Line]);
					}
				}
			}

			numblockingnodes = linedefs2nodes.Values.Count(n => n.IsBlocking);

			Parallel.ForEach(linedefs2nodes.Keys, ld =>
			{
				foreach (Sidedef sd in ld.Front.Sector.Sidedefs)
				{
					if (sd.Line != ld && CheckLinedefValidity(sd.Line))
						linedefs2nodes[ld].Neighbors.Add(linedefs2nodes[sd.Line]);
				}

				foreach (Sidedef sd in ld.Back.Sector.Sidedefs)
				{
					if (sd.Line != ld && CheckLinedefValidity(sd.Line))
						linedefs2nodes[ld].Neighbors.Add(linedefs2nodes[sd.Line]);
				}
			});

#if DEBUG
			int bla = linedefs2nodes.Values.Sum(n => n.Neighbors.Count);
			Console.WriteLine($"There are {linedefs2nodes.Keys.Count} nodes with {bla} interconnections.");
#endif
		}

		private void PopulateStartEndNeighbors(Sector sector, SoundNode node)
		{
			foreach(Sidedef sd in sector.Sidedefs)
			{
				if(CheckLinedefValidity(sd.Line) && linedefs2nodes.ContainsKey(sd.Line))
				{
					node.Neighbors.Add(linedefs2nodes[sd.Line]);
					linedefs2nodes[sd.Line].Neighbors.Add(node);
				}
			}
		}

		public bool FindLeak()
		{
			Finished = false;

			while (true)
			{
				List<SoundNode> openset = new List<SoundNode>() { Start };

				while (openset.Count > 0)
				{
					SoundNode current = openset[0];
					for (int i = 1; i < openset.Count; i++)
					{
						if (openset[i].F < current.F)
							current = openset[i];
					}

					if (current == End)
					{
						Finished = true;
						return true;
					}

					openset.Remove(current);

					current.ProcessNeighbors(openset, Start);
				}

				int currentnumblockingnodes = 0;

				foreach(SoundNode sn in Nodes)
				{
					if(sn.IsBlocking && sn.G != double.MaxValue)
					{
						sn.IsSkip = true;
						currentnumblockingnodes++;
					}

					sn.Reset();
				}

				Start.G = 0.0;
				Start.F = Start.H;

				if(currentnumblockingnodes == numblockingnodes)
				{
					Finished = true;

					return false;
				}
			}
		}
	}
}
