using System;
using LSMEmprunts.Dialogs;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Windows.Input;

namespace LSMEmprunts
{
    public sealed partial class PasswordDlgViewModel : ModalDialogViewModelBase<string>
    {
        public PasswordDlgViewModel()
        {
            OkCommand = ReactiveCommand.Create(() => CloseCommand.Execute(Password).Subscribe());
            CancelCommand = ReactiveCommand.Create(() => CloseCommand.Execute(null).Subscribe());
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        [Reactive]
        private string _Password;

        
    }
}