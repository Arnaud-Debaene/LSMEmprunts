using Microsoft.Xaml.Behaviors;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace LSMEmprunts
{
    /// <summary>
    /// WPF behavior to translate QWERTY input of Key.D0 --> Key.D9 to the corresponding '0' --> '9' characters, independently of the keyboard layout
    /// </summary>
    public class TranslateKeyboardInputBehavior : Behavior<TextBoxBase>
    {
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

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.KeyDown += AssociatedObject_KeyDown;
            AssociatedObject.PreviewTextInput += AssociatedObject_PreviewTextInput;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewTextInput -= AssociatedObject_PreviewTextInput;
            AssociatedObject.KeyDown -= AssociatedObject_KeyDown;            
            base.OnDetaching();
        }

        private Key _LastPressedKey;

        private void AssociatedObject_KeyDown(object sender, KeyEventArgs e)
        {
            _LastPressedKey = e.Key;
            //System.Diagnostics.Debug.WriteLine($"{_LastPressedKey}");
        }

        private void AssociatedObject_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (_LastPressedKey == Key.Return)
            {
                e.Handled = true;
            }
            else if (_KeyMapping.TryGetValue(_LastPressedKey, out var c))
            {
                e.Handled = true;  //prevent the "normal" text to be inputed in the AssociatedObject
                AssociatedObject.AppendText(c);
            }
        }
        
    }
}
