using System.Threading.Tasks;
using System.Windows.Input;
using MvvmDialogs.ViewModels;
using ReactiveUI;

namespace LSMEmprunts
{
    public sealed class ConfirmWindowViewModel : ModalDialogViewModelBase
    {
        public string Message { get; }

        public ConfirmWindowViewModel(string msg)
        {
            Message = msg;
            ConfirmCommand=ReactiveCommand.Create(ConfirmCmd);
        }

        private readonly TaskCompletionSource<bool> _ResultTask = new TaskCompletionSource<bool>();
        public Task<bool> Result => _ResultTask.Task;

        private bool _Confirmed;

        public override void RequestClose()
        {
            _ResultTask.SetResult(_Confirmed);
            base.RequestClose();
        }

        public ICommand ConfirmCommand { get; }

        private void ConfirmCmd()
        {
            _Confirmed = true;
            RequestClose();
        }
    }
}
