using FluentValidation;
using System;
using System.Collections.Generic;


namespace LSMEmprunts
{
    public static class FluentValidators
    {
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
                    if (item.Equals(validatabeObject))
                    {
                        continue;
                    }
                    if (propertyExtractor(item).Equals(propertyValue))
                    {
                        return false;
                    }
                }
                return true;
            });
        }

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
