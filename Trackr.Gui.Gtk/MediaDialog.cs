using System;
using System.Diagnostics;
using Gdk;
using Gtk;
using Pango;

using Trackr.Api;
using Image = Gtk.Image;

namespace Trackr.Gui.Gtk {
	/// <summary>
	/// A dialog for displaying a media entry.
	/// </summary>
	/// <typeparam name="T">The type of media</typeparam>
	public abstract class MediaDialog<T> : Dialog where T : ApiEntry{
		/// <summary>
		/// A copy of the media with updated changes.
		/// </summary>
		public T Result;

		/// <summary>
		/// The original media entry
		/// </summary>
		protected T Original;
		/// <summary>
		/// Has the media been changed?
		/// </summary>
		protected bool Changed;
		/// <summary>
		/// Should we create a new entry in the list?
		/// </summary>
		protected bool Create;
		
		protected Notebook Nb;
		protected HBox Header, EpisodeCounter;
		protected ScrolledWindow ListWindow, ViewWindow;
		protected Table HeaderTable, ListTable, ViewTable;

		protected ComboBox StatusBox;
		
		protected Button OkButton, CancelButton, AddRemoveButton;
		
		protected MediaDialog(T t) {
			Original = t;
			Title = Program.GetTitle(t);
			TransientFor = Program.Win;
			DestroyWithParent = true;
			WindowPosition = WindowPosition.CenterOnParent;
			Role = "Media";
			TypeHint = WindowTypeHint.Dialog;
			DefaultSize = new Size(500, 450);

			Build();

			DeleteEvent += delegate { Respond(ResponseType.Cancel); };
		}

		private void Build() {
			EpisodeCounter = new HBox(false, 0); // Put a SpinButton for episodes in here if needed
			HeaderTable = new Table(0, 0, false); // set column and row counts based on specific needs
			Header = BuildHeader();
			Nb = new Notebook();
			ListWindow = new ScrolledWindow();
			ViewWindow = new ScrolledWindow();
			ListTable = BuildListTable();
			
			//Make the notebook
			ListWindow.Add(new Viewport(){ ListTable });
			ViewWindow.Add(new Viewport(){ ViewTable });
			if(Original.ListStatus != ApiEntry.ListStatuses.NotInList)
				Nb.AppendPage(ListWindow, new Label("List"));
			Nb.AppendPage(ViewWindow, new Label("Details"));
			
			// Pack everything
			VBox.PackStart(Header, false, false, 5);
			VBox.Add(Nb);
			ActionArea.PackEnd(BuildButtons());
		}

		/* We are going to align the header so that an image takes up the left,
		 * and a VBox with general media data takes up the right.			*/
		private HBox BuildHeader() {
			var h = new HBox(false, 5);
			var imagebox = new VBox();
			var databox = new VBox();
			
			// let's fetch the cover
			try {
				var stream = Original.GetCover().Result;
				var cover = new Pixbuf(stream, 113, 159);
				imagebox.PackStart(new Image(cover), false, false, 0);
			}
			catch(Exception e) {
				Debug.WriteLine("Render failed! " + e.Message);
			}
			finally {
				h.PackStart(imagebox, false, false, 0);
			}
			
			// build the title
			var font = new FontDescription() {
				Weight = Weight.Bold,
				Size = (int)(14 * Pango.Scale.PangoScale)
			};
			var title = new Label(Program.GetTitle(Original));
			title.ModifyFont(font);
			databox.PackStart(title, true, false, 0);
			
			// For keeping track of episodes (optional)
			databox.Add(EpisodeCounter);
			
			// For other various media data
			databox.Add(HeaderTable);
			
			// compile it all together
			h.PackEnd(databox);
			return h;
		}

		private Table BuildListTable() {
			var options = new[] {"Currently Watching", "Completed", "On Hold", "Dropped", "Planned"};

			var t = new Table(1, 9, false);
			StatusBox = new ComboBox(options) {Active = (int)Original.ListStatus - 1};

			t.Attach(new Label("List Status"), 0, 1, 0, 1);
			t.Attach(StatusBox, 2, 3, 0, 1);
			StatusBox.Changed += OnStatusChanged;
			return t;
		}

		private HBox BuildButtons() {
			var h = new HBox(false, 5);
			
			OkButton = new Button("OK");
			CancelButton = new Button("Cancel");

			if(Original.ListStatus == ApiEntry.ListStatuses.NotInList) {
				AddRemoveButton = new Button(new Image(Stock.Add, IconSize.Button));
				AddRemoveButton.Clicked += OnAdd;
			}
			else {
				AddRemoveButton = new Button(new Image(Stock.Remove, IconSize.Button));
				AddRemoveButton.Clicked += OnRemove;
			}
			
			h.Add(AddRemoveButton);
			h.Add(OkButton);
			h.Add(CancelButton);
			
			OkButton.Clicked += OnOk;
			CancelButton.Clicked += OnCancel;
			AddRemoveButton.SetSizeRequest(30, 30);
			OkButton.SetSizeRequest(70, 30);
			CancelButton.SetSizeRequest(70, 30);
			OkButton.GrabDefault();

			return h;
		}

		protected void OnStatusChanged(object o, EventArgs args) {
			// 0 is NotInList! fix the offset!
			Result.ListStatus = (ApiEntry.ListStatuses)(StatusBox.Active + 1);
			Changed = true;
		}

		protected void OnAdd(object o, EventArgs args) {
			Create = true;
			Nb.InsertPage(ListWindow, new Label("List"), 0);
			Nb.CurrentPage = 0;
			AddRemoveButton.Visible = false;
		}

		protected void OnRemove(object o, EventArgs args) {
			var d = new MessageDialog(Program.Win, DialogFlags.DestroyWithParent, MessageType.Question, ButtonsType.YesNo, "Delete the selected media?");
			if(d.Run() == (int)ResponseType.Yes) {
				Result.ListStatus = ApiEntry.ListStatuses.NotInList;
				Respond(ResponseType.Apply);
			}
			d.Destroy();
		}

		protected void OnOk(object o, EventArgs args) {
			if(Create)
				Respond(ResponseType.Accept);
			else if(Changed)
				Respond(ResponseType.Apply);
			else Respond(ResponseType.Cancel);
		}
		
		protected void OnCancel(object o, EventArgs args) {
			Respond(ResponseType.Cancel); 
		}
	}
}