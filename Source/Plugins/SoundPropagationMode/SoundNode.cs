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

using System.Collections.Generic;
using System.Drawing;
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
			IsBlocking = linedef.IsFlagSet(SoundPropagationMode.BlockSoundFlag);
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

		internal void RenderPath(IRenderer2D renderer)
		{
			SoundNode current = this;

			while(current != null)
			{
				if (current != this && current.From != null)
				{
					RectangleF rectangle = new RectangleF((float)(current.Position.x - 4 / renderer.Scale), (float)(current.Position.y - 4 / renderer.Scale), 8 / renderer.Scale, 8 / renderer.Scale);
					renderer.RenderRectangleFilled(rectangle, PixelColor.FromColor(Color.Red), true);
				}

				if(current.From != null)
					renderer.RenderLine(current.Position, current.From.Position, 1.0f, PixelColor.FromColor(Color.Red), true);

				current = current.From;
			}
		}
	}
}
