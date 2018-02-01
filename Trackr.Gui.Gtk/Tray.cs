using System;
using Gdk;
using Gtk;

namespace Trackr.Gui.Gtk {
	public class SystemTray : StatusIcon {
		public SystemTray() {
			Pixbuf = Program.AppIcon;
			Tooltip = Program.AppName;
			Visible = true;
			Menu menu = new Popup();
			
			Activate += delegate { Program.Win.Visible = true; };
			PopupMenu += delegate { menu.Popup(); };
		}

		private class Popup : Menu {
			internal Popup() {
				var open = new MenuItem("Open");
				var settings = new MenuItem("Settings");
				settings.Activated += OnSettings;
				var quit = new MenuItem("Quit");
				
				open.Activated += delegate { Program.Win.Visible = true; };
				quit.Activated += delegate {
					Application.Quit();
					Environment.Exit(0);
				};
				
				Add(open);
				Add(settings);
				Add(quit);
				ShowAll();
			}

			private void OnSettings(object o, EventArgs args) {
				var s = new SettingsWindow(false);
				if(s.Run() == (int) ResponseType.Accept) {
					Program.SettingsChanged();
				}
				s.Destroy();
			}
		}
	}
}