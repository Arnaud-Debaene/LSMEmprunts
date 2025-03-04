﻿using System;
using System.ComponentModel.DataAnnotations;

namespace LSMEmprunts
{
    internal sealed class GreaterThanAttribute : ValidationAttribute
    {
        public GreaterThanAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var instance = validationContext.ObjectInstance;
            var otherValue = instance.GetType().GetProperty(PropertyName).GetValue(instance);

            if (((IComparable)value).CompareTo(otherValue) > 0)
            {
                return ValidationResult.Success;
            }

            return new("Valeur trop petite");
        }
    }
}