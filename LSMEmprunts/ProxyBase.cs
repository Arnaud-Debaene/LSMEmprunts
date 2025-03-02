using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LSMEmprunts
{
    public abstract class ProxyBase<WrappedType, DerivedType> : INotifyPropertyChanged, INotifyDataErrorInfo, IEditableObject
        where WrappedType : class
        where DerivedType : ProxyBase<WrappedType, DerivedType>
    {
        public readonly WrappedType WrappedElt;

        protected ProxyBase(WrappedType inner)
        {
            WrappedElt = inner;
            System.Diagnostics.Debug.Assert(this.GetType().IsAssignableTo(typeof(DerivedType)));
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(Expression<Func<WrappedType, T>> selector, T value, bool validate= true, [CallerMemberName] string propertyName = null)
        {
            var memberExpression = selector.Body as MemberExpression;
            if (memberExpression == null)
            {
                throw new InvalidEnumArgumentException("selector shall be a non static = property of WrappedType");
            }
            var propertyInfo = memberExpression.Member as PropertyInfo;
            if (propertyInfo == null || propertyInfo.GetMethod.IsStatic)
            {
                throw new InvalidEnumArgumentException("selector shall be a non static property of WrappedType");
            }

            var currentValue = propertyInfo.GetValue(WrappedElt);
            if (Equals(currentValue, value))
            {
                return false;
            }

            propertyInfo.SetValue(WrappedElt, value);
            RaisePropertyChanged(propertyName);
            if (validate)
            {
                ValidateAllProperties();
            }
            return true;
        }

        protected bool SetProperty<T>(ref T backingField, T value, bool validate = true, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(backingField, value))
            {
                backingField = value;
                RaisePropertyChanged(propertyName);
                if (validate)
                {
                    ValidateAllProperties();
                }
                return true;
            }
            return false;
        }

        private void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        #region INotifyDataErrorInfo implementation

        protected virtual IValidator<DerivedType> Validator { get; } = null;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return null;
            }
            if (_ErrorsPerProperty.TryGetValue(propertyName, out List<ValidationFailure> retval))
            {
                return retval.Select(e => e.ErrorMessage);
            }
            return null;
        }

        public bool HasErrors => _ErrorsPerProperty.Any(e => e.Value.Count > 0);

        private readonly Dictionary<string, List<ValidationFailure>> _ErrorsPerProperty = new();

        protected void ValidateAllProperties()
        {
            var validator = Validator;
            if (validator != null)
            {
                var currentErrors = new Dictionary<string, List<ValidationFailure>>(_ErrorsPerProperty);
                _ErrorsPerProperty.Clear();
                var validationResult = validator.Validate((DerivedType)this);
                foreach (var validationErrorsPerProperty in validationResult.Errors.GroupBy(e => e.PropertyName))
                {
                    _ErrorsPerProperty.Add(validationErrorsPerProperty.Key, new List<ValidationFailure>(validationErrorsPerProperty));
                }

                RaiseErrorsChangedIfReallyChanged(currentErrors, _ErrorsPerProperty);
                RaiseErrorsChangedIfReallyChanged(_ErrorsPerProperty, currentErrors);
            }
        }

        private void RaiseErrorsChangedIfReallyChanged(Dictionary<string, List<ValidationFailure>> errors1, Dictionary<string, List<ValidationFailure>> errors2)
        { 
            foreach((var propName, var errorsPerProps1) in errors1)
            {
                if (!errors2.TryGetValue(propName, out var errorsPerProps2))
                {
                    RaiseErrorsChanged(propName);
                }
                else
                {
                    foreach(var error in errorsPerProps1)
                    {
                        if (!errorsPerProps2.Any(e=>e.ErrorMessage == error.ErrorMessage))
                        {
                            RaiseErrorsChanged(propName);
                            break;
                        }
                    }
                }
            }
        }

        private void RaiseErrorsChanged(string propertyName) => ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        #endregion

        #region IEditableObject implementation
        private Memento<WrappedType> _Memento;

        public void BeginEdit()
        {
            if (_Memento==null)
            {
                _Memento = new Memento<WrappedType>(WrappedElt);
            }
        }

        public void EndEdit()
        {
            _Memento = null;
        }

        public void CancelEdit()
        {
            if (_Memento!=null)
            {
                _Memento.Restore(WrappedElt);
                foreach(var propName in _Memento.SavedProperties)
                {
                    RaisePropertyChanged(propName);
                }
                _Memento = null;
                ValidateAllProperties();
            }
        }
        #endregion
    }
}
