using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeImp.DoomBuilder.Map;

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	internal class LeakFinder
	{
		public SoundNode Start { get; }
		public SoundNode End { get; }
		private SoundPropagationDomain Domain { get; }
		public List<SoundNode> Nodes { get; }

		private Dictionary<Linedef, SoundNode> linedefs2nodes;

		public LeakFinder(Sector source, Sector destination, SoundPropagationDomain domain)
		{
			if (!(domain.Sectors.Contains(source) || domain.AdjacentSectors.Contains(source)) && !(domain.Sectors.Contains(destination) || domain.AdjacentSectors.Contains(destination)))
				throw new ArgumentException("Sound propagation domain does not contain both the start and end sectors");

			Domain = domain;

			End = new SoundNode(destination.Labels[0].position);
			Start = new SoundNode(source.Labels[0].position, End);

			Nodes = new List<SoundNode>() { Start, End };

			linedefs2nodes = new Dictionary<Linedef, SoundNode>();

			GenerateNodes(domain.Sectors);
			GenerateNodes(domain.AdjacentSectors);

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

			bool front = Domain.Sectors.Contains(linedef.Back.Sector) || Domain.AdjacentSectors.Contains(linedef.Back.Sector);
			bool back = Domain.Sectors.Contains(linedef.Back.Sector) || Domain.AdjacentSectors.Contains(linedef.Back.Sector);

			return front && back;
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

				// Pass 2: populate neighbors
				foreach(Sidedef sd1 in sidedefs)
				{
					foreach(Sidedef sd2 in sidedefs)
					{
						if (sd1 != sd2)
							linedefs2nodes[sd1.Line].Neighbors.Add(linedefs2nodes[sd2.Line]);
					}
				}
			}
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
	}
}
