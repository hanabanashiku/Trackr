using Gtk;
using Pango;

namespace Trackr.Gui.Gtk {
	internal class NullAccountWindow : VBox {

		internal NullAccountWindow() : base(false, 3) {
			BorderWidth = 20;
			var attr = new AttrList();
			attr.Insert(new AttrScale(Pango.Scale.Large));
			attr.Insert(new AttrWeight(Weight.Bold));
			Add(new Label("Hang on, there!") {
				Justify = Justification.Center,
				Attributes = attr
			});
			Add(new Label("We need an account to be able to display any useful data here. \n " +
			              "To continue, please add a default account in the application settings."));
			Add(new VBox());
			Add(new VBox());
		}
		
	}
}