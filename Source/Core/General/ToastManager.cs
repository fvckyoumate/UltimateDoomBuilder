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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Controls;

namespace CodeImp.DoomBuilder
{
	public enum ToastType
	{
		INFO,
		WARNING,
		ERROR
	}

	internal enum ToastAnchor
	{
		TOPLEFT = 1,
		TOPRIGHT,
		BOTTOMRIGHT,
		BOTTOMLEFT
	}

	internal class ToastManager
	{
		#region ================== Variables

		private List<ToastControl> toasts;
		private Control bindcontrol;
		private Timer timer;
		private Dictionary<string, bool> registeredactions;

		#endregion

		#region ================== Constructors

		public ToastManager(Control bindcontrol)
		{
			toasts = new List<ToastControl>();

			this.bindcontrol = bindcontrol;

			// Create the timer that will handle moving the toasts. Do not start it, though
			timer = new Timer();
			timer.Interval = 1;
			timer.Tick += UpdateEvent;

			// Find actions that are registered for toasts and load their visibility setting
			registeredactions = new Dictionary<string, bool>();
			foreach (Actions.Action action in General.Actions.GetAllActions().Where(a => a.RegisterToast))
			{
				registeredactions[action.Name] = General.Settings.ReadSetting($"toasts.visibility.{action.Name}", true);
			}
		}

		#endregion

		#region ================== Events

		private void UpdateEvent(object sender, EventArgs args)
		{
			if (toasts.Count == 0)
				return;

			// Go through all toasts and check if they should decay or not. Remove toasts that reached their lifetime
			for (int i = toasts.Count - 1; i >= 0; i--)
			{
				toasts[i].CheckDecay();

				if (!toasts[i].IsAlive())
				{
					bindcontrol.Controls.Remove(toasts[i]);
					toasts.RemoveAt(i);
				}
			}

			// No toasts left, so we should stop the timer
			if (toasts.Count == 0)
			{
				timer.Stop();
				return;
			}

			ToastControl ft = toasts[0];
			ToastAnchor anchor = GetAnchorFromNumber(General.Settings.ToastPosition);

			// We only need to update the first toasts if it didn't reach it end position yet
			bool needsupdate =
				((anchor == ToastAnchor.TOPLEFT || anchor == ToastAnchor.TOPRIGHT) && ft.Location.Y != ft.Margin.Top)
				||
				((anchor == ToastAnchor.BOTTOMLEFT || anchor == ToastAnchor.BOTTOMRIGHT) && ft.Location.Y != bindcontrol.Height - ft.Height - ft.Margin.Bottom)
			;

			if(needsupdate)
			{
				int left;
				int top;

				if (anchor == ToastAnchor.TOPLEFT || anchor == ToastAnchor.BOTTOMLEFT)
					left = ft.Margin.Right;
				else
					left = bindcontrol.Width - ft.Width - ft.Margin.Right;

				// This moves the toast up or down a bit, depending on its anchor position. How fast this happens depends on
				// the control's height, i.e. no matter the height a toast will always take the same time to slide in
				if (anchor == ToastAnchor.TOPLEFT || anchor == ToastAnchor.TOPRIGHT)
					top = ft.Location.Y + ft.Height / 5;
				else
					top = ft.Location.Y - ft.Height / 5;

				Point newLocation = new Point(left, top);

				// If the movement overshot the final position snap it back to the final position
				if ((anchor == ToastAnchor.BOTTOMLEFT || anchor == ToastAnchor.BOTTOMRIGHT) && newLocation.Y < bindcontrol.Height - ft.Height - ft.Margin.Bottom)
					newLocation.Y = bindcontrol.Height - ft.Height - ft.Margin.Bottom;
				else if ((anchor == ToastAnchor.TOPLEFT || anchor == ToastAnchor.TOPRIGHT) && newLocation.Y > ft.Margin.Top)
					newLocation.Y = ft.Margin.Top;

				ft.Location = newLocation;
			}

			if (toasts.Count > 1)
			{
				// Align all other toasts to their predecessor
				for (int i = 1; i < toasts.Count; i++)
				{
					int top;

					if (anchor == ToastAnchor.TOPLEFT || anchor == ToastAnchor.TOPRIGHT)
						top = toasts[i - 1].Bottom + toasts[i - 1].Margin.Bottom;
					else
						top = toasts[i - 1].Location.Y - toasts[i].Height - toasts[i].Margin.Bottom;

					toasts[i].Location = new Point(
						ft.Location.X,
						top
					);
				}
			}
		}

		#endregion

		#region ================== Methods

		/// <summary>
		/// Gets the ToastAnchor from a number. Makes sure the input is valid, otherwise returns a default.
		/// </summary>
		/// <param name="number">The number</param>
		/// <returns>The appropriate ToastAnchor, or BOTTOMRIGHT if input is not valid</returns>
		private ToastAnchor GetAnchorFromNumber(int number)
		{
			return Enum.IsDefined(typeof(ToastAnchor), number) ? (ToastAnchor)number : ToastAnchor.BOTTOMRIGHT;
		}

		/// <summary>
		/// Adds a new toast.
		/// </summary>
		/// <param name="type">Toast type</param>
		/// <param name="text">The message body of the toast</param>
		public void AddToast(ToastType type, string text)
		{
			if (!General.Settings.ToastsEnabled)
				return;

			string title = "Information";

			if (type == ToastType.WARNING)
				title = "Warning";
			else if (type == ToastType.ERROR)
				title = "Error";

			AddToast(type, title, text);
		}

		/// <summary>
		/// Adds a new toast.
		/// </summary>
		/// <param name="type">Toast type</param>
		/// <param name="title">Title of the toast</param>
		/// <param name="text">The message body of the toast</param>
		public void AddToast(ToastType type, string title, string text)
		{
			if (!General.Settings.ToastsEnabled)
				return;

			if(General.Actions.Current != null)
			{
				if (General.Settings.ToastActionsEnabled.ContainsKey(General.Actions.Current.Name) && General.Settings.ToastActionsEnabled[General.Actions.Current.Name] == false)
					return;
			}

			ToastControl tc = new ToastControl(type, title, text, General.Settings.ToastDuration);
			ToastAnchor anchor = GetAnchorFromNumber(General.Settings.ToastPosition);

			// Set the initial y position of the control so that it's outside of the control the toast manager is bound to.
			// No need to care about the x position, since that will be set in the update event anyway
			if (anchor == ToastAnchor.TOPLEFT || anchor == ToastAnchor.TOPRIGHT)
				tc.Location = new Point(0, -tc.Height);
			else
				tc.Location = new Point(0, bindcontrol.Height);

			toasts.Insert(0, tc);
			bindcontrol.Controls.Add(tc);

			// Need to set the toast to be at the front, otherwise the new control would be behind the control the toast manager
			// is bound to
			bindcontrol.Controls.SetChildIndex(tc, 0);

			// Start the timer so that the toast is moved into view
			if (!timer.Enabled)
				timer.Start();
		}

		#endregion
	}
}
