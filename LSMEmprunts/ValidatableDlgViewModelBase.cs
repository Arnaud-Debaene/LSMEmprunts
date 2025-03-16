using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using MvvmDialogs.ViewModels;
using ReactiveUI;

namespace LSMEmprunts
{
    public abstract class ValidatableDlgViewModelBase<DerivedType> : ModalDialogViewModelBase, INotifyDataErrorInfo
        where DerivedType : ValidatableDlgViewModelBase<DerivedType>
    {
        private readonly FluentWpfValidator<DerivedType> _Validator;

        protected ValidatableDlgViewModelBase(FluentWpfValidator<DerivedType> validator) 
        {
            _Validator = validator;
            Changed.Subscribe(_ => HandlePropertyChanged());
        }


        #region INotifyDataErrorInfo implementation

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public bool HasErrors => _Validator?.HasErrors ?? false;

        public IEnumerable GetErrors(string propertyName) => _Validator?.GetErrors(propertyName) ?? Enumerable.Empty<string>();

        private void HandlePropertyChanged()
        {
            var changes = _Validator.ValidateAllProperties((DerivedType)this, out var hasErrorsHasChanged);
            if (hasErrorsHasChanged)
            {
                this.RaisePropertyChanged(nameof(HasErrors));
            }
            foreach (var change in changes)
            {
                ErrorsChanged?.Invoke(this, change);
            }
        }
        #endregion
    }
}
