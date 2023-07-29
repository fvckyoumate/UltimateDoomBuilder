using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

		private ConcurrentDictionary<Linedef, SoundNode> linedefs2nodes;
		private int numblockingnodes;

		public LeakFinder(Sector source, Vector2D sourceposition, Sector destination, Vector2D destinationposition, HashSet<Sector> sectors)
		{
			if (!sectors.Contains(source) || !sectors.Contains(destination))
				throw new ArgumentException("Sound propagation domain does not contain both the start and end sectors");

			End = new SoundNode(destinationposition);
			Start = new SoundNode(sourceposition, End) { G = 0 };
			Sectors = sectors;

			Nodes = new List<SoundNode>() { Start, End };

			linedefs2nodes = new ConcurrentDictionary<Linedef, SoundNode>();

			Stopwatch sw = Stopwatch.StartNew();

			GenerateNodes(sectors);

			sw.Stop();
			Console.WriteLine($"GenerateNodes took {sw.ElapsedMilliseconds} ms");

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
			Stopwatch sw1 = new Stopwatch();
			Stopwatch sw2 = new Stopwatch();

			foreach(Sector s in sectors)
			{
				IEnumerable<Sidedef> sidedefs = s.Sidedefs.Where(sd => CheckLinedefValidity(sd.Line));

				// Pass 1: create nodes
				sw1.Start();
				foreach(Sidedef sd in sidedefs)
				{
					if(!linedefs2nodes.ContainsKey(sd.Line))
					{
						linedefs2nodes[sd.Line] = new SoundNode(sd.Line, End);
						Nodes.Add(linedefs2nodes[sd.Line]);
					}
				}
				sw1.Stop();

				// Pass 2: populate neighbors
				/*
				foreach(Sidedef sd1 in sidedefs)
				{
					foreach(Sidedef sd2 in sidedefs)
					{
						if (sd1 != sd2)
							linedefs2nodes[sd1.Line].Neighbors.Add(linedefs2nodes[sd2.Line]);
					}
				}
				*/
			}

			numblockingnodes = linedefs2nodes.Values.Count(n => n.IsBlocking);

			sw2.Start();
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
			sw2.Stop();

			Console.WriteLine($"Pass 1 took {sw1.ElapsedMilliseconds} ms");
			Console.WriteLine($"Pass 2 took {sw2.ElapsedMilliseconds} ms");

			int bla = linedefs2nodes.Values.Sum(n => n.Neighbors.Count);
			Console.WriteLine($"There are {linedefs2nodes.Keys.Count} nodes with {bla} interconnections.");

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
			Stopwatch sw = Stopwatch.StartNew();

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
						sw.Stop();
						Console.WriteLine($"FindLeak took {sw.ElapsedMilliseconds} ms");
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
					sw.Stop();
					Console.WriteLine($"FindLeak took {sw.ElapsedMilliseconds} ms");

					return false;
				}

			}
		}
	}
}
