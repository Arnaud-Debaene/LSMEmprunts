using FluentValidation;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections;
using System.ComponentModel;

namespace LSMEmprunts.Dialogs
{
    /// <summary>
    /// Base class for the view model of a modal dialog that supports validation (and implement INotifyDataErrorInfo)
    /// </summary>
    /// <typeparam name="DerivedType">the derived type</typeparam>
    public abstract partial class ValidatableDlgViewModelBase<DerivedType, TResult> : ModalDialogViewModelBase<TResult>, INotifyDataErrorInfoImpl<DerivedType>
        where DerivedType : ValidatableDlgViewModelBase<DerivedType, TResult>
    {
        private readonly FluentErrorHelper<DerivedType> _ErrorHelper;

        protected ValidatableDlgViewModelBase(AbstractValidator<DerivedType> validator) 
        {
            _ErrorHelper = new FluentErrorHelper<DerivedType>(validator, (DerivedType)this);
            Changed.Subscribe(_ => _ErrorHelper.ValidateAllProperties()); //whenever a property changes, run the validation
        }


        #region INotifyDataErrorInfoImpl implementation

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        [Reactive(SetModifier = AccessModifier.Private)]
        private bool _HasErrors;

        public void SetHasError(bool b) => HasErrors = b;

        public void RaiseErrorsChanged(DataErrorsChangedEventArgs args) => ErrorsChanged?.Invoke(this, args);

        public IEnumerable GetErrors(string propertyName) => _ErrorHelper.GetErrors(propertyName);

        #endregion
    }
}
