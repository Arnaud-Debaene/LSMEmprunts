using MvvmDialogs.ViewModels;
using System;
using System.Windows.Threading;

namespace LSMEmprunts
{
    public sealed class WarningWindowViewModel : ModalDialogViewModelBase
    {
        public string Message { get; }

        private static readonly TimeSpan Duration = TimeSpan.FromSeconds(4);

        public WarningWindowViewModel(string msg)
        {
            Message = msg;
        }

        private DispatcherTimer _Timer;

        public void OnViewLoaded()
        {
            _Timer = new DispatcherTimer { Interval = Duration };
            _Timer.Tick += OnTimerElapsed;
            _Timer.Start();
        }

        private void OnTimerElapsed(object sender, EventArgs e)
        {
            _Timer.Stop();
            RequestClose();
        }
    }
}
