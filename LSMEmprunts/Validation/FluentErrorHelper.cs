using FluentValidation;
using FluentValidation.Results;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LSMEmprunts
{
    /// <summary>
    /// Interface that must be implemented by an object that wishes to implement INotifyDataErrorInfo through the FluentErrorHelper class. 
    /// This interface provides some methods that will be called by FluentErrorHelper to update the HasErrors property and raise 
    /// the ErrorsChanged event when the validation errors change.
    /// </summary>
    /// <typeparam name="TItem">Type of the derived class that will use FluentHelper</typeparam>
    interface INotifyDataErrorInfoImpl<TItem> : INotifyDataErrorInfo
        where TItem: INotifyDataErrorInfoImpl<TItem>
    {
        void SetHasError(bool b);
        void RaiseErrorsChanged(DataErrorsChangedEventArgs args);
    }

    /// <summary>
    /// An helper class to implement IErrorNotifyDataErrorInfo on an object that is validated through a FluentValidator. 
    /// This class will store the validation errors per property, and trigger the ErrorsChanged event for each property that has changed errors 
    /// (added or removed) when ValidateAllProperties is called.
    /// </summary>
    /// <typeparam name="TItem">Type of the object to validate</typeparam>
    class FluentErrorHelper<TItem> where TItem : INotifyDataErrorInfoImpl<TItem>
    {
        private readonly AbstractValidator<TItem> _Validator;

        private readonly TItem _ValidatableObject;

        /// <summary>
        /// store for the current validation errors per property. 
        /// The key is the property name, and the value is the list of validation failures for that property.
        /// </summary>
        private readonly Dictionary<string, List<ValidationFailure>> _ErrorsPerProperty = [];

        public FluentErrorHelper(AbstractValidator<TItem> validator, TItem validatableObject)
        {
            _Validator = validator;
            _ValidatableObject = validatableObject;
        }

        /// <summary>
        /// Helper for implementing INotifyDataErrorInfo.GetErrors in the _ValidatableObject
        /// </summary>
        /// <remarks>
        /// TItem.GetErrors shall delegate its implementation to this method
        /// </remarks>
        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //return all errors
                return _ErrorsPerProperty.Values.SelectMany(e => e).Select(e => e.ErrorMessage);
            }
            if (_ErrorsPerProperty.TryGetValue(propertyName, out List<ValidationFailure> retval))
            {
                return retval.Select(e => e.ErrorMessage);
            }
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Run a validation of all the properties of _ValidatableObject, updating _ErrorsPerProperty 
        /// and raising _ValidatableObject.ErrorsChanged as required
        /// </summary>
        public void ValidateAllProperties()
        {
            if (_Validator != null)
            {
                var changes = new HashSet<DataErrorsChangedEventArgs>();
                var currentErrors = new Dictionary<string, List<ValidationFailure>>(_ErrorsPerProperty);
                _ErrorsPerProperty.Clear();

                var validationResult = _Validator.Validate(_ValidatableObject);
                foreach (var validationErrorsPerProperty in validationResult.Errors.GroupBy(e => e.PropertyName))
                {
                    _ErrorsPerProperty.Add(validationErrorsPerProperty.Key, [.. validationErrorsPerProperty]);
                }

                FillErrorsChangesList(currentErrors, _ErrorsPerProperty, changes);
                FillErrorsChangesList(_ErrorsPerProperty, currentErrors, changes);

                _ValidatableObject.SetHasError(_ErrorsPerProperty.Count>0);
                foreach (var change in changes)
                {
                    _ValidatableObject.RaiseErrorsChanged(change);
                }
            }
        }

        private static void FillErrorsChangesList(Dictionary<string, List<ValidationFailure>> errors1, Dictionary<string, List<ValidationFailure>> errors2, HashSet<DataErrorsChangedEventArgs> list)
        {
            foreach ((var propName, var errorsPerProps1) in errors1)
            {
                if (!errors2.TryGetValue(propName, out var errorsPerProps2))
                {
                    list.Add(new DataErrorsChangedEventArgs(propName));
                }
                else
                {
                    foreach (var error in errorsPerProps1)
                    {
                        if (!errorsPerProps2.Any(e => e.ErrorMessage == error.ErrorMessage))
                        {
                            list.Add(new DataErrorsChangedEventArgs(propName));
                            break;
                        }
                    }
                }
            }
        }


    }
}
