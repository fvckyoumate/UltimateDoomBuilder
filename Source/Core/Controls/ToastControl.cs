using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ToastControl : UserControl
	{
		private long startime;
		private long lifetime;
		bool pausedecay;

		public ToastControl(ToastType type, string title, string text, long lifetime = 3000)
		{
			InitializeComponent();

			this.lifetime = lifetime;
			startime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

			// Set icon
			if(type == ToastType.INFO)
				icon.BackgroundImage = SystemIcons.Information.ToBitmap();
			else if(type == ToastType.WARNING)
				icon.BackgroundImage = SystemIcons.Warning.ToBitmap();
			else if(type == ToastType.ERROR)
				icon.BackgroundImage = SystemIcons.Error.ToBitmap();

			lbTitle.Text = title;
			lbText.Text = text;

			// The text label is auto-size, but we need to programatically set a max width so that longer texts are
			// automatically broken into multiple lines
			lbText.MaximumSize = new Size(Width - lbText.Location.X - Margin.Right, lbText.MaximumSize.Height);

			// Resize the height of the control if the text doesn't fit vertically
			if (lbText.Location.Y + lbText.Height + Margin.Bottom > Height)
				Height = lbText.Location.Y + lbText.Height + lbTitle.Location.Y + Margin.Bottom;

			pausedecay = false;
		}

		/// <summary>
		/// Checks if the toast is decaying, i.e. the cursor is currently not inside the control.
		/// </summary>
		public void CheckDecay()
		{
			if (ClientRectangle.Contains(PointToClient(Cursor.Position)))
			{
				pausedecay = true;
			}
			else if(pausedecay)
			{
				pausedecay = false;

				// Reset the start time, so that the control will only die "lifetime" ms after the cursor left the control
				startime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			}
		}

		/// <summary>
		/// Checks if the control is still "alive" (has not reached its lifetime).
		/// </summary>
		/// <returns>true if it's alive, false if it isn't</returns>
		public bool IsAlive()
		{
			if (!pausedecay && DateTimeOffset.Now.ToUnixTimeMilliseconds() - startime > lifetime)
				return false;

			return true;
		}
	}
}
