using ReactiveUI;
using System.Reactive;
using System.Windows;

namespace LSMEmprunts.Dialogs
{
    /// <summary>
    /// Manages the display and interaction of modal dialogs and common file/message dialogs.
    /// Provides a reactive-based dialog system that supports custom modal dialogs and standard Windows dialogs.
    /// </summary>
    public interface IDialogManager
    {
        /// <summary>
        /// Registers a custom interaction handler for a modal dialog.
        /// </summary>
        /// <typeparam name="TViewModel">The type of the view model for the dialog, must derive from <see cref="ModalDialogViewModelBase{TResult}"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result returned by the dialog.</typeparam>
        /// <param name="interaction">The interaction to register handlers for.</param>
        void RegisterInteraction<TViewModel, TResult>(IInteraction<TViewModel, TResult> interaction) where TViewModel : ModalDialogViewModelBase<TResult>;

        #region interactions to display common dialogs
        /// <summary>
        /// Gets the interaction for displaying a file save dialog.
        /// </summary>
        Interaction<SaveFileDialogViewModel, bool> SaveFile { get; }

        /// <summary>
        /// Gets the interaction for displaying a file open dialog.
        /// </summary>
        Interaction<OpenFileDialogViewModel, bool> OpenFile { get; }

        /// <summary>
        /// Gets the interaction for displaying a message box.
        /// </summary>
        Interaction<MessageBoxViewModel, MessageBoxResult> MessageBox { get; }

        /// <summary>
        /// Gets the interaction for displaying a confirmation dialog.
        /// </summary>
        Interaction<ConfirmWindowViewModel, bool> ConfirmWindow { get; }

        /// <summary>
        /// Gets the interaction for displaying a warning dialog.
        /// </summary>
        Interaction<WarningWindowViewModel, Unit> WarningWindow { get; }
        #endregion

    }
}
