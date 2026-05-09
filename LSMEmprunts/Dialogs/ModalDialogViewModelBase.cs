using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;

namespace LSMEmprunts.Dialogs
{
    public abstract class ModalDialogViewModelBase<TResult> : ReactiveObject
    {
        public ModalDialogViewModelBase()
        {
            CloseCommand = ReactiveCommand.Create<TResult, Unit>((result) =>
            {
                ResultTaskSource.TrySetResult(result);
                return Unit.Default;
            });
        }

        public ReactiveCommand<TResult, Unit> CloseCommand { get; }

        internal TaskCompletionSource<TResult> ResultTaskSource { get; } = new();

        public Task<TResult> Result => ResultTaskSource.Task;

    }
}
