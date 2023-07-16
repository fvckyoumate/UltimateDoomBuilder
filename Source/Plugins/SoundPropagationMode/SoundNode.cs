using System;
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
		public double F => G + H;
		public bool isBlocking;
		public bool skip;

		public SoundNode(Vector2D position)
		{
			Position = position;
			G = double.MaxValue;
			H = double.MaxValue;
			isBlocking = false;
			skip = false;
			Neighbors = new List<SoundNode>();
		}

		public SoundNode(Vector2D position, SoundNode destination): this(position)
		{
			H = Vector2D.Distance(Position, destination.Position);
		}

		public SoundNode(Linedef linedef, SoundNode destination) : this(linedef.Line.GetCoordinatesAt(0.5), destination)
		{
			isBlocking = General.Map.UDMF ? linedef.IsFlagSet("blocksound") : linedef.IsFlagSet("64");
		}

		internal void Render(IRenderer2D renderer)
		{
			RectangleF rectangle = new RectangleF((float)(Position.x - 10), (float)(Position.y - 10), 20, 20);
			renderer.RenderRectangleFilled(rectangle, PixelColor.FromColor(Color.Red), true);

			foreach (SoundNode sn in Neighbors)
				renderer.RenderLine(Position, sn.Position, 1.0f, PixelColor.FromColor(Color.Red), true);
		}

	}
}
