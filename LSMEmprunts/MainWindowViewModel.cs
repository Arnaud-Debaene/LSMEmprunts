using LSMEmprunts.Dialogs;
using ReactiveUI;
using Splat;
using System;
using System.Windows;
using System.Windows.Threading;

namespace LSMEmprunts
{
    public sealed class MainWindowViewModel : ReactiveObject, IScreen
    {
        public RoutingState Router { get; } = new();
        

        public MainWindowViewModel()
        {
            Application.Current.DispatcherUnhandledException += OnUnhandledException;

            // Navigate to the first page
            Router.Navigate.Execute(new HomeViewModel(this)).Subscribe();
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var vm = new MessageBoxViewModel
            {
                Caption = "Erreur",
                Buttons = MessageBoxButton.OK,
                Message = e.Exception.CompleteDump()
            };
            Locator.Current.GetService<IDialogManager>().MessageBox.Handle(vm);
        }

    }
}
