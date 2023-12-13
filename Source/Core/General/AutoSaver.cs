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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeImp.DoomBuilder
{
	internal enum AutosaveResult
	{
		Success,
		Error,
		NoFileName
	}

	internal class AutoSaver
	{
		private static long lasttime;
		private static System.Windows.Forms.Timer timer;

		internal void InitializeTimer()
		{
			if(timer != null)
			{
				timer.Tick -= TryAutosave;
				timer.Dispose();
				timer = null;
			}

			if (General.Settings.Autosave)
			{
				lasttime = Clock.CurrentTime;
				timer = new System.Windows.Forms.Timer() { Interval = 1000 };
				timer.Tick += TryAutosave;
				timer.Enabled = true;
			}
		}

		internal void StopTimer()
		{
			if (timer != null) timer.Enabled = false;
		}

		internal void ResetTimer()
		{
			lasttime = Clock.CurrentTime;
		}

		internal void BeforeClockReset()
		{
			lasttime = -(Clock.CurrentTime - lasttime);
		}

		private static void TryAutosave(object sender, EventArgs args)
		{
			Console.WriteLine($"Trying to save in {(lasttime + 10000) - Clock.CurrentTime}");
			if (Clock.CurrentTime > lasttime + /*General.Settings.AutosaveInterval * 60 */ 10 * 1000)
			{
				if (!General.Editing.Mode.OnAutoSaveBegin())
				{
					Console.WriteLine("Current editing mode prevented autosave!");
					return;
				}

				lasttime = Clock.CurrentTime;
				if (General.Map != null && General.Map.Map != null && General.Map.IsChanged)
				{
					Stopwatch sw = Stopwatch.StartNew();
					AutosaveResult success = General.Map.AutoSave();
					sw.Stop();
					if (success == AutosaveResult.Success)
						General.ToastManager.ShowToast(ToastType.INFO, "Autosave", $"Autosave completed successfully in {sw.ElapsedMilliseconds} ms.");
					else if (success == AutosaveResult.Error)
						General.ToastManager.ShowToast(ToastType.ERROR, "Autosave", "Autosave failed.");
					else if (success == AutosaveResult.NoFileName)
						General.ToastManager.ShowToast(ToastType.WARNING, "Autosave", "Could not autosave because this is a new WAD that wasn't saved yet.");
				}
			}
		}
	}
}
