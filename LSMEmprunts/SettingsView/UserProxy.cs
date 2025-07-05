using FluentValidation;
using LSMEmprunts.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LSMEmprunts
{
    public class UserProxy : ProxyBase<User, UserProxy>
    {
        private readonly IEnumerable<UserProxy> _Collection;

        public UserProxy(User data, IEnumerable<UserProxy> collection)
            : base(data, MyValidator.Instance)
        {
            _Collection = collection;
            ValidateAllProperties();
        }

        public int Id => WrappedElt.Id;

        public string Name
        {
            get => WrappedElt.Name;
            set
            {                
                SetProperty(e => e.Name, value);
            }
        }

        public string Phone
        {
            get => WrappedElt.Phone;
            set => SetProperty(e => e.Phone, value);
        }

        private int _StatsBorrowsCount;
        public int StatsBorrowsCount
        {
            get => _StatsBorrowsCount;
            private set => SetProperty(ref _StatsBorrowsCount, value, false, false);
        }

        internal void UpdateStats(IEnumerable<Borrowing> history, DateTime now)
        {
            StatsBorrowsCount = history.Count();
        }

        private class MyValidator : FluentWpfValidator<UserProxy>
        {
            private MyValidator() 
            {
                RuleFor(e=>e.Name).NotEmpty().WithMessage("Nom requis");
                RuleFor(e=>e.Name).ItemUnique(user=>user._Collection).WithMessage("le nom doit être unique");
            }

            public static MyValidator Instance { get; } = new MyValidator();
        }
    }
}
