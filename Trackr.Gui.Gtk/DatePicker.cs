using System;
using Gdk;
using Gtk;
using Image = Gtk.Image;

namespace Trackr.Gui.Gtk {
    /// <summary>
    /// A widget used for selecting and displaying a calendar date.
    /// </summary>
    public class DatePicker : HBox {

        private DateTime _value;
        /// <summary>
        /// The widget's current value
        /// </summary>
        public DateTime Value {
            get => _value;
            set {
                _value = value;
                _display.Text = _value.ToString("MMM dd, yyyy");
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public new bool Sensitive {
            get => _calendarButton.Visible;
            set {
                _calendarButton.Visible = value;
                _todayButton.Visible = value;
            }
        }

        /// <summary>
        /// Called when the value is changed
        /// </summary>
        public EventHandler Changed;

        private Label _display;
        private Button _calendarButton, _todayButton;
        
        /// <param name="date">The initial date</param>
        public DatePicker(DateTime date) {
            Build();
            _value = date;
            Sensitive = true;
            _display.Text = _value.ToString("MMM dd, yyyy");
        }

        private void Build() {
            _display = new Label();
            var buf = Pixbuf.LoadFromResource("Trackr.Gui.Gtk.Resources.icons.calendar.png");
            _calendarButton = new Button(new Image(buf));
            _todayButton = new Button("Insert Today");
            
            PackStart(_display, false, false, 0);
            PackStart(_calendarButton, false, false, 5);
            _calendarButton.SetSizeRequest(30, 30);
            Add(_todayButton);
            _calendarButton.Clicked += OnCalendarClick;
            _todayButton.Clicked += delegate { Value = DateTime.Today; };
        }

        private void OnCalendarClick(object o, EventArgs args) {
            var d = new CalendarDialog();
            if (d.Run() == (int) ResponseType.Accept)
               Value = d.Result;
            d.Destroy();
        }

        private class CalendarDialog : Dialog {
            public DateTime Result;

            public CalendarDialog(){
                var calendar = new Calendar();
                var ok = new Button(Stock.Ok);
                var cancel = new Button(Stock.Cancel);
                
                VBox.Add(calendar);
                calendar.DaySelected += delegate { Result = calendar.Date; };
                
                ActionArea.Add(ok);
                ok.Clicked += delegate { Respond(ResponseType.Accept); };
                ActionArea.Add(cancel);
                cancel.Clicked += delegate { Respond(ResponseType.Cancel); };
                ShowAll();
            }
        }
    }
}