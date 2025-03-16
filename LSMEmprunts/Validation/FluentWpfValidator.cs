using FluentValidation;
using FluentValidation.Results;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LSMEmprunts
{
    /// <summary>
    /// helper class to implement INotifyDataErrorInfo based on a FluentValidation validator
    /// </summary>
    public class FluentWpfValidator<T> : AbstractValidator<T>
    {
        private readonly Dictionary<string, List<ValidationFailure>> _ErrorsPerProperty = new();

        public bool HasErrors => _ErrorsPerProperty.Any(e => e.Value.Count > 0);

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //return all errors
                return _ErrorsPerProperty.Values.SelectMany(e=>e).Select(e=>e.ErrorMessage);
            }
            if (_ErrorsPerProperty.TryGetValue(propertyName, out List<ValidationFailure> retval))
            {
                return retval.Select(e => e.ErrorMessage);
            }
            return null;
        }

        public IEnumerable<DataErrorsChangedEventArgs> ValidateAllProperties(T instance, out bool hasErrorsHasChanged)
        {
            var retval = new HashSet<DataErrorsChangedEventArgs>();
            hasErrorsHasChanged = false;

            if (instance!=null)
            {
                var hadErrors = HasErrors;

                var currentErrors = new Dictionary<string, List<ValidationFailure>>(_ErrorsPerProperty);
                _ErrorsPerProperty.Clear();

                var validationResult = Validate(instance);
                foreach (var validationErrorsPerProperty in validationResult.Errors.GroupBy(e => e.PropertyName))
                {
                    _ErrorsPerProperty.Add(validationErrorsPerProperty.Key, new List<ValidationFailure>(validationErrorsPerProperty));
                }

                FillErrorsChangesList(currentErrors, _ErrorsPerProperty, retval);
                FillErrorsChangesList(_ErrorsPerProperty, currentErrors, retval);

                hasErrorsHasChanged = hadErrors != HasErrors;
            }
            return retval;
        }

        private void FillErrorsChangesList(Dictionary<string, List<ValidationFailure>> errors1, Dictionary<string, List<ValidationFailure>> errors2, HashSet<DataErrorsChangedEventArgs> list)
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
