using LSMEmprunts.Dialogs;

namespace LSMEmprunts
{
    public sealed class ConfirmWindowViewModel : OkCancelDlgViewModelBase
    {
        public string Message { get; }

        public ConfirmWindowViewModel(string msg)
        {
            Message = msg;
        }
    }
}
