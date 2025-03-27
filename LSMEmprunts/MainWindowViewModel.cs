using MvvmDialogs;
using MvvmDialogs.ViewModels;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LSMEmprunts
{
    public sealed class MainWindowViewModel : ReactiveObject
    {
        public static MainWindowViewModel Instance { get; } = new MainWindowViewModel();

        private MainWindowViewModel()
        {
            SetCurrentPage(new HomeViewModel()).Wait();

            Application.Current.DispatcherUnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var vm = new MessageBoxViewModel
            {
                Caption = "Erreur",
                Buttons = MessageBoxButton.OK,
                Message = e.Exception.CompleteDump()
            };
            Dialogs.Add(vm);
        }

        private ReactiveObject _CurrentPageViewModel;
        public ReactiveObject CurrentPageViewModel => _CurrentPageViewModel;

        public async Task SetCurrentPage(ReactiveObject viewModel)
        {
            if (_CurrentPageViewModel is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (_CurrentPageViewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }

            this.RaiseAndSetIfChanged(ref _CurrentPageViewModel, viewModel, nameof(CurrentPageViewModel));
        }

        public ObservableCollection<IDialogViewModel> Dialogs { get; } = new ObservableCollection<IDialogViewModel>();
    }
}
