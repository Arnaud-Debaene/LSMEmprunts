using FluentValidation;
using System;
using System.Collections.Generic;


namespace LSMEmprunts
{
    /// <summary>
    /// Provides reusable FluentValidation extension methods for common validation rules.
    /// </summary>
    public static class FluentValidators
    {
        /// <summary>
        /// Adds a uniqueness rule for a property within a collection extracted from the validated object.
        /// The rule passes when no other item in the collection (except the validated object) has the same
        /// property value as the current property value.
        /// </summary>
        /// <typeparam name="T">The type of the validated object.</typeparam>
        /// <typeparam name="TProperty">The type of the property being validated. Must implement <see cref="IEquatable{TProperty}"/>.</typeparam>
        /// <param name="ruleBuilder">The rule builder for the property being validated.</param>
        /// <param name="collectionExtractor">Function that extracts the collection of items from the validated object to check against.</param>
        /// <returns>An <see cref="IRuleBuilderOptions{T, TProperty}"/> to continue configuring the rule.</returns>
        public static IRuleBuilderOptions<T, TProperty> ItemUnique<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, 
            Func<T, IEnumerable<T>> collectionExtractor) 
            where T : class
            where TProperty : IEquatable<TProperty>
        {
            //extract/rebuild the property extractor function from the rule that is being build
            var rule = DefaultValidatorOptions.Configurable(ruleBuilder);
            //IRule gives us the property extraction functor as an expression : Compile it to a lambda/delegate
            var propertyExtractor = (Func<T, TProperty>)(rule.Expression.Compile());

            return ruleBuilder.Must((validatabeObject, propertyValue) =>
            {
                var collection = collectionExtractor(validatabeObject);
                foreach (var item in collection)
                {
                    if (item == validatabeObject)
                    {
                        continue;
                    }
                    if (Comparer<TProperty>.Default.Compare(propertyExtractor(item), propertyValue) == 0)
                    {
                        return false;
                    }
                }
                return true;
            });
        }

        /// <summary>
        /// Adds a uniqueness rule for items within a collection extracted from the validated object using a custom comparer.
        /// The rule passes when no other item in the collection (except the validated object) is considered equal by the comparer.
        /// </summary>
        /// <typeparam name="T">The type of the validated object.</typeparam>
        /// <typeparam name="TProperty">The type of the property being validated. Must implement <see cref="IEquatable{TProperty}"/>.</typeparam>
        /// <param name="ruleBuilder">The rule builder for the property being validated.</param>
        /// <param name="collectionExtractor">Function that extracts the collection of items from the validated object to check against.</param>
        /// <param name="itemsComparer">Comparer function that receives an item and the validated object and returns true when they should be considered equal.</param>
        /// <returns>An <see cref="IRuleBuilderOptions{T, TProperty}"/> to continue configuring the rule.</returns>
        public static IRuleBuilderOptions<T, TProperty> ItemUnique<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder,
            Func<T, IEnumerable<T>> collectionExtractor, Func<T, T, bool> itemsComparer)
            where T : class
            where TProperty : IEquatable<TProperty>
        {
            return ruleBuilder.Must((validatabeObject, propertyValue) =>
            {
                var collection = collectionExtractor(validatabeObject);
                foreach (var item in collection)
                {
                    if (item.Equals(validatabeObject))
                    {
                        continue;
                    }
                    if (itemsComparer(item, validatabeObject))
                    {
                        return false;
                    }
                }
                return true;
            });
        }
    }
}
