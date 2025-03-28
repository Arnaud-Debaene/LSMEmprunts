using Microsoft.Xaml.Behaviors;
using System.Text.RegularExpressions;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace LSMEmprunts
{
    /// <summary>
    /// Behavior on a text box to refuse any input that is not a number
    /// </summary>
    public sealed class OnlyNumbersAllowedBehavior : Behavior<TextBoxBase>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewTextInput += OnPreviewTextInput;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
            base.OnDetaching();
        }

        private static readonly Regex _Regex = new("^[0-9]+$");

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_Regex.IsMatch(e.Text);
        }
    }
}
