using DynamicData;
using FluentValidation;
using LSMEmprunts.Data;
using LSMEmprunts.EditableGridView;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LSMEmprunts
{
    /// <summary>
    /// An MVVM friendly wrapper around a User, for edition in the Settings view
    /// </summary>
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

        private class MyValidator : AbstractValidator<UserProxy>
        {
            private MyValidator() 
            {
                RuleFor(e=>e.Name).NotEmpty().WithMessage("Nom requis");
                RuleFor(e=>e.Name).ItemUnique(user=>user._Collection).WithMessage("le nom doit être unique");
            }

            public static MyValidator Instance { get; } = new MyValidator();
        }
    }

    /// <summary>
    /// ViewModel for the list of available Users in the Settings view
    /// </summary>
    public class UsersListViewModel : GridHostViewModel<UserProxy>
    {
        public UsersListViewModel(IEnumerable<User> users)
        {
            var proxies = users.Select(u => BuildProxy(u));
            _Items.AddRange(proxies);
        }

        public void Add(User user) => _Items.Add(BuildProxy(u: user));

        public void Remove(UserProxy proxy)
        {
            _Items.Remove(proxy);
            ValidateAll();
        }

        override protected void OnCommitEdit() => ValidateAll();

        protected override void OnCancelEdit() => ValidateAll();

        private void ValidateAll()
        {
            foreach(var item in Items)
            {
                item.ValidateAllProperties();
            }
        }

        private UserProxy BuildProxy(User u) => new(u, Items);
    }
}
