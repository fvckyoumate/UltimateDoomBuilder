#region ================== Copyright (c) 2022 Boris Iwanski

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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;

namespace CodeImp.DoomBuilder.UDBScript.Wrapper
{
	internal class ModeWrapper
	{
		#region ================== Enums

		internal enum RenderLayers : int
		{
			None = 0,
			Background = 1,
			Plotter = 2,
			Things = 3,
			Overlay = 4,
			Surface = 5
		}

		#endregion

		#region ================== Variables

		private ScriptedMode mode;
		private RenderLayers renderLayer;
		private bool clearPlotter;

		#endregion

		#region ================== Properties

		internal bool ClearPlotter { get { return clearPlotter; } set { clearPlotter = value; } }

		#endregion

		#region ================== Constructors

		internal ModeWrapper(ScriptedMode mode)
		{
			this.mode = mode;
			renderLayer = RenderLayers.None;

			//clearPlotter = true;
		}

		#endregion

		#region ================== Methods

		internal void ResetRenderLayer()
		{
			renderLayer = RenderLayers.None;
		}

		private void VerifyRenderLayer(RenderLayers targetLayer)
		{
			if(renderLayer != targetLayer)
			{
				if (renderLayer != RenderLayers.None)
					mode.Renderer.Finish();

				switch(targetLayer)
				{
					case RenderLayers.Plotter:
						mode.Renderer.StartPlotter(clearPlotter);
						renderLayer = RenderLayers.Plotter;
						clearPlotter = false;
						break;
				}
			}
		}

		public bool StartPlotter(bool clear)
		{
			return mode.Renderer.StartPlotter(clear);
		}

		public void FinishRenderer()
		{
			mode.Renderer.Finish();
		}

		public void Redraw()
		{
			General.Interface.RedrawDisplay();
		}

		public void Accept()
		{
			General.Editing.AcceptMode();
		}

		public void PlotLinedef(LinedefWrapper ld)
		{
			VerifyRenderLayer(RenderLayers.Plotter);

			mode.Renderer.PlotLinedef(ld.Linedef, mode.Renderer.DetermineLinedefColor(ld.Linedef));
		}

		public void PlotLinedefSet(LinedefWrapper[] linedefs)
		{
			VerifyRenderLayer(RenderLayers.Plotter);

			Linedef[] lds = new Linedef[linedefs.Length];

			for (int i = 0; i < linedefs.Length; i++)
				lds[i] = linedefs[i].Linedef;
			
			mode.Renderer.PlotLinedefSet(lds);
		}

		public void PlotLine(object p1, object p2, byte r, byte g, byte b)
		{
			VerifyRenderLayer(RenderLayers.Plotter);

			PixelColor c = new PixelColor(255, r, g, b);

			Vector2D v1 = (Vector2D)BuilderPlug.Me.GetVectorFromObject(p1, false);
			Vector2D v2 = (Vector2D)BuilderPlug.Me.GetVectorFromObject(p2, false);

			mode.Renderer.PlotLine(v1, v2, c /*, BuilderPlug.LINE_LENGTH_SCALER */);
		}
		#endregion
	}
}
