using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace LSMEmprunts
{
    /// <summary>
    /// A behavior to capture the keyboard input on the associated object as long as the Active dependency property is true
    /// </summary>
    /// <remarks>
    /// Focus is not really captured. Rather, whenever the associated object looses the focus, a timer is started that will refocus it after a short time.
    /// </remarks>
    public sealed class CaptureKeyboardBehavior : Behavior<FrameworkElement>
    {
        private readonly DispatcherTimer _RefocusTimer =new();

        public CaptureKeyboardBehavior()
        {
            _RefocusTimer.Tick += OnRefocusTimerTick;
            _RefocusTimer.Interval = TimeSpan.FromMilliseconds(800);
        }


        #region Active dependency property
        public static readonly DependencyProperty ActiveProperty = DependencyProperty.Register(nameof(Active), typeof(bool), typeof(CaptureKeyboardBehavior),
            new PropertyMetadata(true, OnActiveChanged));


        public bool Active
        {
            get => (bool)GetValue(ActiveProperty);
            set => SetValue(ActiveProperty, value);
        }

        private static void OnActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var me = (CaptureKeyboardBehavior)d;
            if (me._Attached && (bool)e.NewValue)
            {
                Keyboard.Focus(me.AssociatedObject);
            }
        }

        #endregion

        private bool _Attached = false;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewLostKeyboardFocus += OnPreviewLostKeyboardFocus;
            _Attached = true;

            //note the the set focus action must be executd with a low priority, so that it executes late AFTER the control is initialized
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, (Action)(() => Keyboard.Focus(AssociatedObject)));
        }

        protected override void OnDetaching()
        {
            _RefocusTimer.Tick -= OnRefocusTimerTick;

            _Attached = false;
            AssociatedObject.PreviewLostKeyboardFocus -= OnPreviewLostKeyboardFocus;
            base.OnDetaching();
        }

        private void OnPreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            //schedule an operation that will give us back the focus
            if (Active)
            {
                _RefocusTimer.Start();
            }
        }

       
        private void OnRefocusTimerTick(object sender, EventArgs e)
        {
            _RefocusTimer.Stop();
            if (Active)
            {
                Keyboard.Focus(AssociatedObject);
            }
        }
    }
}
