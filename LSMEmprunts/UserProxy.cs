using LSMEmprunts.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LSMEmprunts
{
    public class UserProxy : ProxyBase<User>
    {
        private readonly IEnumerable<UserProxy> _Collection;

        public UserProxy(User data, IEnumerable<UserProxy> collection)
            : base(data)
        {
            _Collection = collection;
            EvaluateNameValidity();
            EvaluateLicenceScanIdValidity();
        }

        public int Id => WrappedElt.Id;

        public string Name
        {
            get => WrappedElt.Name;
            set
            {                
                SetProperty(e => e.Name, value);
                EvaluateNameValidity();
            }
        }

        private void EvaluateNameValidity()
        {
            ClearErrors(nameof(Name));
            if (string.IsNullOrEmpty(Name))
            {
                AddError(nameof(Name), "Nom requis");
            }
            else if (_Collection.Any(e => e != this && e.Name == Name))
            {
                AddError(nameof(Name), "le nom doit être unique");
            }
        }

        public string LicenceScanId
        {
            get => WrappedElt.LicenceScanId;
            set
            {
                SetProperty(e => e.LicenceScanId, value);
                EvaluateLicenceScanIdValidity();
            }
        }

        private void EvaluateLicenceScanIdValidity()
        {
            ClearErrors(nameof(LicenceScanId));
            if (string.IsNullOrEmpty(LicenceScanId))
            {
                AddError(nameof(LicenceScanId), "Code barre Licence requis");
            }
            else if (_Collection.Any(e => e != this && e.LicenceScanId == LicenceScanId))
            {
                AddError(nameof(LicenceScanId), "le code licence doit être unique");
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
            private set => SetProperty(ref _StatsBorrowsCount, value);
        }

        internal void UpdateStats(IEnumerable<Borrowing> history, DateTime now)
        {
            StatsBorrowsCount = history.Count();
        }
    }
}
