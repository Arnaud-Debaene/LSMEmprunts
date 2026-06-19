using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;

namespace LSMEmprunts.Dialogs
{
    /// <summary>
    /// Abstract base class for modal dialog view models that need to return a result to the caller.
    /// Provides a reactive command for closing the dialog and a task-based result mechanism.
    /// Derived classes should implement the view model logic specific to their dialog type.
    /// </summary>
    /// <typeparam name="TResult">The type of the result that will be returned when the dialog closes.</typeparam>
    public abstract class ModalDialogViewModelBase<TResult> : ReactiveObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModalDialogViewModelBase{TResult}"/> class.
        /// Initializes the CloseCommand reactive command that closes the dialog with a result.
        /// </summary>
        public ModalDialogViewModelBase()
        {
            // Create a reactive command that accepts a result parameter and closes the dialog
            // When executed, the command sets the result on the ResultTaskSource,
            // allowing the dialog manager to retrieve the result asynchronously
            CloseCommand = ReactiveCommand.Create<TResult, Unit>((result) =>
            {
                // Set the result on the task completion source, completing the Result task
                ResultTaskSource.TrySetResult(result);
                return Unit.Default;
            });
        }

        /// <summary>
        /// Gets the reactive command for closing the dialog with a result.
        /// When executed with a result value, the dialog will close and return that result to the caller.
        /// </summary>
        public ReactiveCommand<TResult, Unit> CloseCommand { get; }

        /// <summary>
        /// Gets the task completion source used internally to track the dialog result.
        /// Allows the dialog manager to await the completion of the dialog and retrieve its result.
        /// </summary>
        internal TaskCompletionSource<TResult> ResultTaskSource { get; } = new();

        /// <summary>
        /// Gets a task that completes when the dialog closes, providing access to the dialog's result.
        /// Callers can await this task to synchronously wait for the dialog to complete and retrieve its result.
        /// </summary>
        public Task<TResult> Result => ResultTaskSource.Task;

    }
}
