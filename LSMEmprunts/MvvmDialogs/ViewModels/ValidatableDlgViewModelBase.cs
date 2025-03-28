using System;
using System.Collections;
using System.ComponentModel;
using MvvmDialogs.ViewModels;
using ReactiveUI;

namespace LSMEmprunts
{
    /// <summary>
    /// Base class for the view model of a modal dialog that supports validation (and implement INotifyDataErrorInfo)
    /// </summary>
    /// <typeparam name="DerivedType">the derived type</typeparam>
    /// <remarks>
    /// The real implementation of INotifyDataErrorInfo is provided by _Validator. The public members that implement INotifyDataErrorInfo simply call it
    /// </remarks>
    public abstract class ValidatableDlgViewModelBase<DerivedType> : ModalDialogViewModelBase, INotifyDataErrorInfo
        where DerivedType : ValidatableDlgViewModelBase<DerivedType>
    {
        private readonly FluentWpfValidator<DerivedType> _Validator;

        protected ValidatableDlgViewModelBase(FluentWpfValidator<DerivedType> validator) 
        {
            _Validator = validator;
            Changed.Subscribe(_ => HandlePropertyChanged()); //whenver a property changes, run the validation
        }


        #region INotifyDataErrorInfo implementation

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public bool HasErrors => _Validator.HasErrors;

        public IEnumerable GetErrors(string propertyName) => _Validator.GetErrors(propertyName);

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
