using LSMEmprunts.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LSMEmprunts
{
    public class GearProxy : ProxyBase<Gear>
    {
        private readonly IEnumerable<GearProxy> _Collection;

        public GearProxy(Gear data, IEnumerable<GearProxy> collection)
            : base(data)
        {
            _Collection = collection;
            EvaluateNameValidity();
            EvaluateBarCodeValidity();
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
            if (_Collection.Any(e => e != this && e.Name == Name && e.Type == Type))
            {
                AddError(nameof(Name), "Le nom doit être unique pour un type d'équipement");
            }
        }

        public GearType Type
        {
            get => WrappedElt.Type;
            set => SetProperty(e => e.Type, value);
        }

        public string BarCode
        {
            get => WrappedElt.BarCode;
            set
            {
                SetProperty(e => e.BarCode, value);
                EvaluateBarCodeValidity();
            }
        }

        private void EvaluateBarCodeValidity()
        {
            ClearErrors(nameof(BarCode));
            if (string.IsNullOrEmpty(BarCode))
            {
                AddError(nameof(BarCode), "Code barre requis");
            }
            if (_Collection.Any(e => e != this && e.BarCode == BarCode))
            {
                AddError(nameof(BarCode), "le code barre doit être unique");
            }
        }

        public string Size
        {
            get => WrappedElt.Size;
            set
            {
                SetProperty(e=>e.Size, value);
                EvaluateSizeValidity();
            }
        }

        private TimeSpan _StatsBorrowsDuration;
        public TimeSpan StatsBorrowsDuration
        {
            get => _StatsBorrowsDuration;
            private set => SetProperty(ref _StatsBorrowsDuration, value);
        }

        private int _StatsBorrowsCount;
        public int StatsBorrowsCount
        {
            get => _StatsBorrowsCount;
            private set => SetProperty(ref _StatsBorrowsCount , value);
        }

        internal void UpdateStats(IEnumerable<Borrowing> history, DateTime now)
        {
            StatsBorrowsCount = history.Count();
            StatsBorrowsDuration = history.Aggregate(TimeSpan.Zero, (totalDuration, borrowing) =>
            {
                var borrowDuration = (borrowing.ReturnTime ?? now) - borrowing.BorrowTime;
                return totalDuration + borrowDuration;
            });
        }

        public static string[] AllowedTankSizes { get; } = 
        {
            string.Empty, "6L", "7L", "9L", "10L", "12L", "15L", "18L"
        };

        public static string[] AllowedBCDSizes { get; } =
        {
            string.Empty, "Enfant", "XXS", "XS", "S", "M", "L", "XL", "XXL"
        };

        private void EvaluateSizeValidity()
        {
            ClearErrors(nameof(Size));
            if (string.IsNullOrEmpty(Size))
            {
                return;
            }
            switch(Type)
            {
                case GearType.Tank:
                    if (!AllowedTankSizes.Contains(Size))
                    {
                        AddError(nameof(Size), "taille de bloc invalide");
                    }
                    break;
                case GearType.BCD:
                    if (!AllowedBCDSizes.Contains(Size))
                    {
                        AddError(nameof(Size), "taille de stab invalide");
                    }
                    break;
            }
        }
    }
}
