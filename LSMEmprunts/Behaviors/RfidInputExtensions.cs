using ReactiveMarbles.ObservableEvents;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace LSMEmprunts.Behaviors
{
    public static partial class RfidInputExtensions
    {
        /// <summary>
        /// configure a textbox's keyboard handling so that is handles correctly input through RFID scanner
        /// </summary>
        /// <param name="textBox">the textbox to configure</param>
        /// <param name="disposables">a CopistiteDisposable to handle unsusbciption to the requested events</param>
        public static void ConfigureRfidInput(this TextBox textBox, CompositeDisposable disposables)
        {
            //remember last key pressed in the control, il will be used in PreviewTextInput
            textBox.Events().KeyDown.Subscribe(x => _LastKeyPressed[textBox] = x.Key).DisposeWith(disposables);
            
            textBox.Events().PreviewTextInput.Subscribe((TextCompositionEventArgs evt) =>
            {
                if (!_LastKeyPressed.TryGetValue(textBox, out var lastKeyPressed))
                {
                    return;
                }
                else if (_KeyMapping.TryGetValue(lastKeyPressed, out var c))
                {
                    evt.Handled = true;  //prevent the "normal" text to be inputed in the AssociatedObject
                    textBox.AppendText(c);
                }
                else if (!NumberRegex().IsMatch(evt.Text))
                {
                    evt.Handled = true;
                }

            }).DisposeWith(disposables);
        }

        //remember the last key pressed for each textbox
        private static readonly Dictionary<TextBox, Key> _LastKeyPressed = [];

        private static readonly Dictionary<Key, string> _KeyMapping = new()
        {
            {Key.D0, "0" },
            {Key.D1, "1" },
            {Key.D2, "2" },
            {Key.D3, "3" },
            {Key.D4, "4" },
            {Key.D5, "5" },
            {Key.D6, "6" },
            {Key.D7, "7" },
            {Key.D8, "8" },
            {Key.D9, "9" },
        };

        [GeneratedRegex("^[0-9]+$")]
        private static partial Regex NumberRegex();
    }
}
