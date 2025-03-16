using MvvmDialogs.ViewModels;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace LSMEmprunts
{
    public sealed class PasswordDlgViewModel : ModalDialogViewModelBase
    {
        private readonly TaskCompletionSource<string> _ResultTask = new TaskCompletionSource<string>();
        public Task<string> Result => _ResultTask.Task;

        public PasswordDlgViewModel()
        {
            OkCommand = ReactiveCommand.Create<PasswordBox>(OnOk);
            CancelCommand = ReactiveCommand.Create(OnCancel);
        }

        public ReactiveCommand<PasswordBox, Unit> OkCommand { get; }

        public void OnOk(PasswordBox box)
        {
            RequestClose();
            _ResultTask.SetResult(box.Password);
        }

        public ICommand CancelCommand { get; }

        public void OnCancel()
        {
            RequestClose();
            _ResultTask.SetResult(null);
        }
    }
}