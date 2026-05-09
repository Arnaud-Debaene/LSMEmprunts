using LSMEmprunts.Dialogs;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace LSMEmprunts
{
    public sealed class WarningWindowViewModel : ModalDialogViewModelBase<Unit>, IActivatableViewModel
    {
        public ViewModelActivator Activator { get; } = new();

        public string Message { get; }

        public WarningWindowViewModel(string msg)
        {
            Message = msg;

            this.WhenActivated(disposables =>
            {
                var interval = TimeSpan.FromSeconds(4);
                Observable.Timer(interval).Subscribe(_ => CloseCommand.Execute(Unit.Default).Subscribe()).DisposeWith(disposables);
            });
        }

    }
}
