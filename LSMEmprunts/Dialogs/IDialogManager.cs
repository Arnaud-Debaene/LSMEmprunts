using ReactiveUI;
using System.Windows;

namespace LSMEmprunts.Dialogs
{
    public interface IDialogManager
    {
        void RegisterInteraction<TViewModel, TResult>(IInteraction<TViewModel, TResult> interaction) where TViewModel : ModalDialogViewModelBase<TResult>;

        #region interactions to display common dialogs
        Interaction<SaveFileDialogViewModel, bool> SaveFile { get; }
        Interaction<OpenFileDialogViewModel, bool> OpenFile { get; }
        Interaction<MessageBoxViewModel, MessageBoxResult> MessageBox { get; }

        Interaction<ConfirmWindowViewModel, bool> ConfirmWindow { get; }
        #endregion

    }
}
