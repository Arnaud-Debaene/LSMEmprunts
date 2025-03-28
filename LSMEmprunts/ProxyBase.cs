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
    /// <summary>
    /// an MVVM proxy around a business object (typically a DB entity) that provides
    /// - INotfyPropertyChanged implementation
    /// - Validation of the edited object (throug a FluentValidator)
    /// - IEditableObject implementation to support commit / rollback of changes made to the object
    /// </summary>
    /// <typeparam name="WrappedType">The type of twrapped entity</typeparam>
    /// <typeparam name="DerivedType">the derived class</typeparam>
    public abstract class ProxyBase<WrappedType, DerivedType> : INotifyPropertyChanged, INotifyDataErrorInfo, IEditableObject
        where WrappedType : class
        where DerivedType : ProxyBase<WrappedType, DerivedType>
    {
        public readonly WrappedType WrappedElt;

        private readonly FluentWpfValidator<DerivedType> _Validator;

        protected ProxyBase(WrappedType inner, FluentWpfValidator<DerivedType> validator)
        {
            _Validator = validator;
            WrappedElt = inner;
            System.Diagnostics.Debug.Assert(this.GetType().IsAssignableTo(typeof(DerivedType)));
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(Expression<Func<WrappedType, T>> selector, T value, bool validate= true, [CallerMemberName] string propertyName = null)
        {
            var propertyInfo = GetMemberFromExpression(selector);

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


        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName) => _Validator?.GetErrors(propertyName) ?? Enumerable.Empty<string>();

        public bool HasErrors => _Validator?.HasErrors ?? false;

        protected void ValidateAllProperties()
        {
            if (_Validator != null)
            {
                var changes = _Validator.ValidateAllProperties((DerivedType)this, out var hasErrorsChanged);
                if (hasErrorsChanged)
                {
                    RaisePropertyChanged(nameof(HasErrors));
                }
                foreach (var change in changes)
                {
                    ErrorsChanged?.Invoke(this, change);
                }
            }
        }
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

        #region Member access cache
        /// <summary>
        /// Cache to avoid having to analyse over and over again Expressions that describe a property
        /// </summary>
        /// <remarks>
        /// This implemntation is NOT thread-safe (static member, not protected), but since this is an MVVM proxy, it SHALL be used on GUI thread only.
        /// </remarks>
        private static readonly Dictionary<LambdaExpression, PropertyInfo> _MemberToPropertyInfoDict = new();

        private static PropertyInfo GetMemberFromExpression(LambdaExpression lambdaExpression)
        {
            if (!_MemberToPropertyInfoDict.TryGetValue(lambdaExpression, out var propertyInfo))
            {
                var memberExpression = lambdaExpression.Body as MemberExpression;
                if (memberExpression == null)
                {
                    throw new InvalidEnumArgumentException("selector shall be a non static property of WrappedType");
                }
                propertyInfo = memberExpression.Member as PropertyInfo;
                if (propertyInfo == null || propertyInfo.GetMethod.IsStatic)
                {
                    throw new InvalidEnumArgumentException("selector shall be a non static property of WrappedType");
                }
                _MemberToPropertyInfoDict.Add(lambdaExpression, propertyInfo);                
            }

            return propertyInfo;
        }

        #endregion
    }
}
