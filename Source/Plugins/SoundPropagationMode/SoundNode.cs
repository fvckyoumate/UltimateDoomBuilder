using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	internal class SoundNode
	{
		public Vector2D Position { get; set; }
		public List<SoundNode> Neighbors { get; set; }
		public SoundNode From { get; set; }
		public double G { get; set; }
		public double H { get; }
		//public double F => G + H;
		public double F { get; set; }
		public bool IsBlocking { get; }
		public bool IsSkip { get; set; }

		public SoundNode(Vector2D position)
		{
			Position = position;
			G = double.MaxValue;
			H = double.MaxValue;
			IsBlocking = false;
			IsSkip = false;
			Neighbors = new List<SoundNode>();
		}

		public SoundNode(Vector2D position, SoundNode destination): this(position)
		{
			H = Vector2D.Distance(Position, destination.Position);
		}

		public SoundNode(Linedef linedef, SoundNode destination) : this(linedef.Line.GetCoordinatesAt(0.5), destination)
		{
			IsBlocking = General.Map.UDMF ? linedef.IsFlagSet("blocksound") : linedef.IsFlagSet("64");
		}

		//public void ProcessNeighbors(HashSet<SoundNode> openset)
		public void ProcessNeighbors(List<SoundNode> openset, SoundNode start)
		{
			foreach (SoundNode neighbor in Neighbors)
			{
				if ((neighbor.IsBlocking && HasBlockingInPath(start)) || neighbor.IsSkip)
					continue;

				double newg = G + Vector2D.Distance(Position, neighbor.Position);

				if (newg < neighbor.G)
				{
					neighbor.From = this;
					neighbor.G = newg;
					neighbor.F = neighbor.G + neighbor.H;

					if (!openset.Contains(neighbor))
						openset.Add(neighbor);
				}
			}
		}

		private bool HasBlockingInPath(SoundNode start)
		{
			SoundNode current = this;
			while(current != start)
			{
				if (current.IsBlocking)
					return true;
				current = current.From;
			}

			return false;
		}

		public void Reset()
		{
			From = null;
			G = double.MaxValue;
			F = double.MaxValue;
		}

		internal void RenderWithNeighbors(IRenderer2D renderer)
		{
			RectangleF rectangle = new RectangleF((float)(Position.x - 10), (float)(Position.y - 10), 20, 20);
			renderer.RenderRectangleFilled(rectangle, PixelColor.FromColor(Color.Purple), true);

			foreach (SoundNode sn in Neighbors)
				renderer.RenderLine(Position, sn.Position, 1.0f, PixelColor.FromColor(Color.Purple), true);
		}

		internal void RenderPath(IRenderer2D renderer, int dashoffset)
		{
			SoundNode current = this;

			while(current != null)
			{
				RectangleF rectangle = new RectangleF((float)(current.Position.x - 4 / renderer.Scale), (float)(current.Position.y - 4 / renderer.Scale), 8 / renderer.Scale, 8 / renderer.Scale);
				renderer.RenderRectangleFilled(rectangle, PixelColor.FromColor(Color.Red), true);

				if(current.From != null)
					renderer.RenderLine(current.Position, current.From.Position, 1.0f, PixelColor.FromColor(Color.Red), true);

				current = current.From;
			}
		}

	}
}
