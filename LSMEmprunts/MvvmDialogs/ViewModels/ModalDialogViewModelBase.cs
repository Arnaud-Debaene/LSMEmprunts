using ReactiveUI;
using System;
using System.Reactive;

namespace MvvmDialogs.ViewModels
{
    public abstract class ModalDialogViewModelBase : ReactiveObject, IUserDialogViewModel
    {
        protected ModalDialogViewModelBase()
        {
            CloseCommand = ReactiveCommand.Create(RequestClose);
        }

        public virtual bool IsModal => true;

        public event EventHandler DialogClosing;

        public virtual void RequestClose()
        {
            DialogClosing?.Invoke(this, EventArgs.Empty);
        }

        protected bool DialogResult = false;

        public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    }
}
