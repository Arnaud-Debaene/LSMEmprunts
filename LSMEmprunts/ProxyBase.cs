using FluentValidation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LSMEmprunts
{
    /// <summary>
    /// base class for an MVVM proxy around a business object (typically a DB entity) that provides
    /// - INotifyPropertyChanged implementation
    /// - "IsDirty" observable
    /// - Validation of the edited object (throug a FluentValidator)
    /// - IEditableObject implementation to support commit / rollback of changes made to the object
    /// </summary>
    /// <typeparam name="WrappedType">The type of the wrapped entity</typeparam>
    /// <typeparam name="DerivedType">the derived class</typeparam>
    public abstract class ProxyBase<WrappedType, DerivedType> : INotifyPropertyChanged, INotifyDataErrorInfoImpl<DerivedType>, IEditableObject
        where WrappedType : class
        where DerivedType : ProxyBase<WrappedType, DerivedType>
    {
        /// <summary>
        /// The wrapped business object (typically a DB entity) being proxied.
        /// </summary>
        public readonly WrappedType WrappedElt;

        /// <summary>
        /// helper that handles validation of the proxy properties through the FluentValidator,
        /// and raises the ErrorsChanged event when validation errors change.
        /// </summary>
        private readonly FluentErrorHelper<DerivedType> _ErrorHelper;

        /// <summary>
        /// Initializes a new instance of the ProxyBase class.
        /// </summary>
        /// <param name="inner">The wrapped business object to be proxied.</param>
        /// <param name="validator">The FluentValidator instance used for validating the proxy.</param>
        protected ProxyBase(WrappedType inner, AbstractValidator<DerivedType> validator)
        {
            _ErrorHelper = new FluentErrorHelper<DerivedType>(validator, (DerivedType)this);
            WrappedElt = inner;
            System.Diagnostics.Debug.Assert(this.GetType().IsAssignableTo(typeof(DerivedType)));
        }

        #region INotifyPropertyChanged implementation
        /// <summary>
        /// Raised when a property value has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sets a property value on the wrapped object and raises PropertyChanged event.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="selector">A lambda expression selecting the property to set.</param>
        /// <param name="value">The new value for the property.</param>
        /// <param name="validate">Whether to validate all properties after setting.</param>
        /// <param name="set_dirty">Whether to mark the proxy as dirty after setting.</param>
        /// <param name="propertyName">The name of the property (automatically set by CallerMemberName).</param>
        /// <returns>True if the value was changed; false if the new value equals the current value.</returns>
        protected bool SetProperty<T>(Expression<Func<WrappedType, T>> selector, T value, bool validate= true, bool set_dirty = true, [CallerMemberName] string propertyName = null)
        {
            var propertyInfo = GetMemberFromExpression(selector);

            var currentValue = propertyInfo.GetValue(WrappedElt);
            if (Equals(currentValue, value))
            {
                return false;
            }

            propertyInfo.SetValue(WrappedElt, value);
            RaisePropertyChanged(propertyName);
            if (set_dirty)
            {
                SetDirty();
            }
            if (validate)
            {
                ValidateAllProperties();
            }
            return true;
        }

        /// <summary>
        /// Sets a backing field value and raises PropertyChanged event.
        /// </summary>
        /// <typeparam name="T">The type of the backing field.</typeparam>
        /// <param name="backingField">The backing field to set.</param>
        /// <param name="value">The new value for the backing field.</param>
        /// <param name="validate">Whether to validate all properties after setting.</param>
        /// <param name="set_dirty">Whether to mark the proxy as dirty after setting.</param>
        /// <param name="propertyName">The name of the property (automatically set by CallerMemberName).</param>
        /// <returns>True if the value was changed; false if the new value equals the current value.</returns>
        protected bool SetProperty<T>(ref T backingField, T value, bool validate = true, bool set_dirty=true, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(backingField, value))
            {
                backingField = value;
                RaisePropertyChanged(propertyName);
                if (set_dirty)
                {
                    SetDirty();
                }
                if (validate)
                {
                    ValidateAllProperties();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        private void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        #region IsDirty handling
        private bool _IsDirty = false;
        /// <summary>
        /// Gets a value indicating whether the proxy has been modified.
        /// </summary>
        public bool IsDirty => _IsDirty;

        /// <summary>
        /// Marks the proxy as dirty (modified).
        /// </summary>
        internal void SetDirty()
        {
            if (!_IsDirty)
            {
                _IsDirty = true;
                RaisePropertyChanged(nameof(IsDirty));
            }
        }

        #endregion

        #region INotifyDataErrorInfoImpl implementation

        private bool _HasErrors;
        /// <summary>
        /// Gets a value indicating whether the proxy has validation errors.
        /// </summary>
        public bool HasErrors => _HasErrors;

        /// <summary>
        /// Sets whether the proxy has validation errors.
        /// </summary>
        /// <param name="b">True if there are errors; false otherwise.</param>
        public void SetHasError(bool b)
        {
            if (_HasErrors != b)
            {
                _HasErrors = b;
                RaisePropertyChanged(nameof(HasErrors));
            }
        }

        /// <summary>
        /// Raised when the collection of validation errors has changed.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// Raises the ErrorsChanged event.
        /// </summary>
        /// <param name="args">The DataErrorsChangedEventArgs containing information about the errors that changed.</param>
        public void RaiseErrorsChanged(DataErrorsChangedEventArgs args) => ErrorsChanged?.Invoke(this, args);

        /// <summary>
        /// Gets the validation errors for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property to get errors for; null or empty string for object-level errors.</param>
        /// <returns>An enumerable collection of validation errors.</returns>
        public IEnumerable GetErrors(string propertyName) => _ErrorHelper.GetErrors(propertyName);

        /// <summary>
        /// Validates all properties of the proxy.
        /// </summary>
        public void ValidateAllProperties() => _ErrorHelper.ValidateAllProperties();

        #endregion

        #region IEditableObject implementation
        private Memento<WrappedType> _Memento;

        /// <summary>
        /// Begins an edit transaction on the proxy.
        /// </summary>
        public void BeginEdit()
        {
            if (_Memento==null)
            {
                _Memento = new Memento<WrappedType>(WrappedElt);
            }
        }

        /// <summary>
        /// Ends an edit transaction on the proxy, committing all changes.
        /// </summary>
        public void EndEdit()
        {
            _Memento = null;
        }

        /// <summary>
        /// Cancels an edit transaction on the proxy, rolling back all changes.
        /// </summary>
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
                _ErrorHelper.ValidateAllProperties();
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
        private static readonly Dictionary<LambdaExpression, PropertyInfo> _MemberToPropertyInfoDict = [];

        /// <summary>
        /// Extracts the PropertyInfo from a lambda expression that selects a property.
        /// </summary>
        /// <param name="lambdaExpression">A lambda expression selecting a property of WrappedType.</param>
        /// <returns>The PropertyInfo of the selected property.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown if the selector is not a non-static property of WrappedType.</exception>
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
