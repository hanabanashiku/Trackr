using System;
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
		}
	}
}