using ReactiveUI;
using System;
using System.Windows.Input;

namespace LSMEmprunts.Dialogs
{
    public abstract class OkCancelDlgViewModelBase : ModalDialogViewModelBase<bool>
    {
        public OkCancelDlgViewModelBase()
        {
            OkCommand = ReactiveCommand.Create(() => CloseCommand.Execute(true).Subscribe());
            CancelCommand = ReactiveCommand.Create(() => CloseCommand.Execute(false).Subscribe());
        }

        public ICommand OkCommand { get; }

        public ICommand CancelCommand { get; }

    }
}
