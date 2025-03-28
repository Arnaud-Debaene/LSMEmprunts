using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace LSMEmprunts
{
    /// <summary>
    /// A behavior to associate the focus status of a control with an MVVM dependency property
    /// </summary>
    public class SetFocusBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty IsFocusedProperty = DependencyProperty.Register("IsFocused", typeof(bool),
            typeof(SetFocusBehavior), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsFocusedChanged));

        public bool IsFocused
        {
            get => (bool) GetValue(IsFocusedProperty);
            set => SetValue(IsFocusedProperty, value);
        }

        private static void IsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = (SetFocusBehavior) d;

            if (me._DuringOnGotFocusCall)
            {
                return;
            }

            var b = (bool) e.NewValue;
            if (b)
            {
                me.AssociatedObject?.Focus();
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.GotFocus += OnGotFocus;
            AssociatedObject.LostFocus += OnLostFocus;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (IsFocused)
            {
                AssociatedObject.Focus();
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.GotFocus -= OnGotFocus;
            AssociatedObject.LostFocus -= OnLostFocus;
            base.OnDetaching();
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            IsFocused = false;
        }

        private bool _DuringOnGotFocusCall;

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            _DuringOnGotFocusCall = true;
            IsFocused = true;
            _DuringOnGotFocusCall = false;
        }
    }
}
