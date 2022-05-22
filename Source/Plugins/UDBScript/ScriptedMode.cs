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
using System.Drawing;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.UDBScript.Wrapper;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Jint.Runtime.Interop;
using Esprima;

#endregion

namespace CodeImp.DoomBuilder.UDBScript
{
	[EditMode(DisplayName = "Scripted Mode",
			  SwitchAction = "scriptedmode",        // Action name used to switch to this mode
			  ButtonImage = "SoundPropagationIcon.png", // Image resource name for the button
			  ButtonOrder = int.MinValue + 501, // Position of the button (lower is more to the left)
			  ButtonGroup = "000_editing",
			  UseByDefault = true,
			  SafeStartMode = false,
			  Volatile = false)]
	public class ScriptedMode : ClassicMode
	{
		#region ================== Variables

		object highlighted;
		Engine engine;
		private UDBWrapper UDBWrapper;

		private bool clearPlotter;

		#endregion

		#region ================== Properties

		public override object HighlightedObject { get { return highlighted; } }

		public bool ClearPlotter { get { return clearPlotter; } }

		private JsValue onAcceptFunction;
		private JsValue onMouseMoveFunction;
		private JsValue onRedrawDisplayFunction;
		private JsValue onSelectBeginFunction;
		private JsValue onSelectEndFunction;

		#endregion

		#region ================== Constructor / Disposer

		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if (!isdisposed)
			{
				// Dispose base
				base.Dispose();
			}
		}

		#endregion

		#region ================== Methods

		public (bool, bool) GetRenderLayerClearStatus()
		{
			return (clearPlotter, true);
		}

		// This highlights a new item
		private void Highlight(Sector s)
		{
			// Set new highlight
			highlighted = s;
		}

		private void PlotLine(object p1, object p2, byte r, byte g, byte b)
		{
			PixelColor c = new PixelColor(255, r, g, b);

			Vector2D v1 = (Vector2D)BuilderPlug.Me.GetVectorFromObject(p1, false);
			Vector2D v2 = (Vector2D)BuilderPlug.Me.GetVectorFromObject(p2, false);

			renderer.PlotLine(v1, v2, c /*, BuilderPlug.LINE_LENGTH_SCALER */);
		}

		private void Redraw(object o)
		{
			General.Interface.RedrawDisplay();
		}

		private void RunScriptFunction(/*string name*/ JsValue function)
		{
			if (function.IsUndefined())
				return;

			try
			{
				//engine.Invoke(name);
				engine.Invoke(function);
			}
			catch (UserScriptAbortException)
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Script aborted");
			}
			catch (ParserException ex)
			{
				MessageBox.Show("There is an error while parsing the script:\n\n" + ex.Message, "Script error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			catch (Jint.Runtime.JavaScriptException ex)
			{
				if (ex.Error.Type != Jint.Runtime.Types.String)
				{
					//MessageBox.Show("There is an error in the script in line " + e.LineNumber + ":\n\n" + e.Message + "\n\n" + e.StackTrace, "Script error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					UDBScriptErrorForm sef = new UDBScriptErrorForm(ex.Message, ex.StackTrace);
					sef.ShowDialog();
				}
				else
					General.Interface.DisplayStatus(StatusType.Warning, ex.Message); // We get here if "throw" is used in a script
			}
			catch (ExitScriptException ex)
			{
				if (!string.IsNullOrEmpty(ex.Message))
					General.Interface.DisplayStatus(StatusType.Ready, ex.Message);
			}
			catch (DieScriptException ex)
			{
				if (!string.IsNullOrEmpty(ex.Message))
					General.Interface.DisplayStatus(StatusType.Warning, ex.Message);
			}
			catch (Exception ex) // Catch anything else we didn't think about
			{
				UDBScriptErrorForm sef = new UDBScriptErrorForm(ex.Message, ex.StackTrace);
				sef.ShowDialog();
			}
		}

		#endregion

		#region ================== Events

		public override void OnHelp()
		{
			General.ShowHelp("gzdb/features/classic_modes/mode_soundpropagation.html");
		}

		// Cancel mode
		public override void OnCancel()
		{
			// Cancel base class
			base.OnCancel();

			// Return to previous mode
			General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
		}

		// Mode engages
		public override void OnEngage()
		{
			base.OnEngage();

			BuilderPlug.Me.EndOptionEdit();
			General.Interface.Focus();

			General.Map.UndoRedo.CreateUndo("Run scripted mode");

			// Set engine options
			Options options = new Options();
			//options.Constraint(new RuntimeConstraint(stopwatch));
			options.AllowOperatorOverloading();
			options.SetTypeResolver(new TypeResolver
			{
				MemberFilter = member => member.Name != nameof(GetType)
			});

			// Create the script engine
			engine = new Engine(options);
			//engine.SetValue("showMessage", new Action<object>(ShowMessage));
			//engine.SetValue("showMessageYesNo", new Func<object, bool>(ShowMessageYesNo));
			//engine.SetValue("exit", new Action<string>(ExitScript));
			//engine.SetValue("die", new Action<string>(DieScript));
			//engine.SetValue("QueryOptions", TypeReference.CreateTypeReference(engine, typeof(QueryOptions)));
			//engine.SetValue("ScriptOptions", scriptinfo.GetScriptOptionsObject());
			//engine.SetValue("Map", new MapWrapper());
			//engine.SetValue("GameConfiguration", new GameConfigurationWrapper());
			//engine.SetValue("Angle2D", TypeReference.CreateTypeReference(engine, typeof(Angle2DWrapper)));
			//engine.SetValue("Vector3D", TypeReference.CreateTypeReference(engine, typeof(Vector3DWrapper)));
			//engine.SetValue("Vector2D", TypeReference.CreateTypeReference(engine, typeof(Vector2DWrapper)));
			//engine.SetValue("Line2D", TypeReference.CreateTypeReference(engine, typeof(Line2DWrapper)));
			//engine.SetValue("UniValue", TypeReference.CreateTypeReference(engine, typeof(UniValue)));
			//engine.SetValue("Data", TypeReference.CreateTypeReference(engine, typeof(DataWrapper)));

			// These can not be directly instanciated and don't have static method, but it's required to
			// for example use "instanceof" in scripts
			//engine.SetValue("Linedef", TypeReference.CreateTypeReference(engine, typeof(LinedefWrapper)));
			//engine.SetValue("Sector", TypeReference.CreateTypeReference(engine, typeof(SectorWrapper)));
			//engine.SetValue("Sidedef", TypeReference.CreateTypeReference(engine, typeof(SidedefWrapper)));
			//engine.SetValue("Thing", TypeReference.CreateTypeReference(engine, typeof(ThingWrapper)));
			//engine.SetValue("Vertex", TypeReference.CreateTypeReference(engine, typeof(VertexWrapper)));

			UDBWrapper = new UDBWrapper(engine, this);

			engine.SetValue("UDB", UDBWrapper);

			engine.SetValue("plotLine", new Action<object, object, byte, byte, byte>(PlotLine));
			engine.SetValue("redraw", new Action<object>(Redraw));
			engine.SetValue("accept", new Action<object>((o) => General.Editing.AcceptMode()));

#if DEBUG
			engine.SetValue("log", new Action<object>(Console.WriteLine));
#endif

			string script = System.IO.File.ReadAllText(System.IO.Path.Combine(General.AppPath, "udbscript", "modes", "bevelmode.js"));

			engine.Execute(script);
			onAcceptFunction = engine.GetValue("onAccept");
			onMouseMoveFunction = engine.GetValue("onMouseMove");
			onRedrawDisplayFunction = engine.GetValue("onRedrawDisplay");
			onSelectBeginFunction = engine.GetValue("onSelectBegin");
			onSelectEndFunction = engine.GetValue("onSelectEnd");

			/*
			CustomPresentation presentation = new CustomPresentation();
			presentation.AddLayer(new PresentLayer(RendererLayer.Background, BlendingMode.Mask, General.Settings.BackgroundAlpha));
			presentation.AddLayer(new PresentLayer(RendererLayer.Grid, BlendingMode.Mask));
			presentation.AddLayer(new PresentLayer(RendererLayer.Overlay, BlendingMode.Alpha, 1.0f, true));
			presentation.AddLayer(new PresentLayer(RendererLayer.Things, BlendingMode.Alpha, 1.0f));
			presentation.AddLayer(new PresentLayer(RendererLayer.Geometry, BlendingMode.Alpha, 1.0f, true));
			renderer.SetPresentation(presentation);
			*/

			General.Interface.RedrawDisplay();
		}

		// Mode disengages
		public override void OnDisengage()
		{
			base.OnDisengage();

			// Do some updates
			General.Map.Map.Update();
			General.Map.ThingsFilter.Update();

			// Hide highlight info
			General.Interface.HideInfo();
		}

		public override void OnAccept()
		{
			RunScriptFunction(onAcceptFunction);

			General.Editing.ChangeMode(General.Editing.PreviousStableMode.Name);
		}

		// This redraws the display
		public override void OnRedrawDisplay()
		{
			UDBWrapper.Mode.ResetRenderLayer();
			UDBWrapper.Mode.ClearPlotter = true;

			renderer.RedrawSurface();

			// Render lines and vertices
			//if (renderer.StartPlotter(true))
			{
				RunScriptFunction(onRedrawDisplayFunction);
				renderer.Finish();
			}

			// Render things
			if (renderer.StartThings(true))
			{
				renderer.Finish();
			}

			if (renderer.StartOverlay(true))
			{
				renderer.Finish();
			}

			renderer.Present();
		}

		protected override void OnSelectBegin()
		{
			RunScriptFunction(onSelectBeginFunction);
		}

		//mxd. If a linedef is highlighted, toggle the sound blocking flag 
		protected override void OnSelectEnd()
		{
			RunScriptFunction(onSelectEndFunction);
		}

		//mxd
		public override void OnUndoEnd()
		{
			base.OnUndoEnd();

			General.Interface.RedrawDisplay();
		}

		//mxd
		public override void OnRedoEnd()
		{
			base.OnRedoEnd();

			General.Interface.RedrawDisplay();
		}

		// Mouse moves
		public override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			RunScriptFunction(onMouseMoveFunction);
		}

		// Mouse leaves
		public override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);

			// Highlight nothing
			Highlight(null);
		}

		#endregion
	}
}
