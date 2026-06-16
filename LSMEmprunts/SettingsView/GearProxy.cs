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
    /// An MVVM friendly wrapper around a Gear, for edition in the Settings view
    /// </summary>
    public class GearProxy : ProxyBase<Gear, GearProxy>
    {
        private readonly IEnumerable<GearProxy> _Collection;

        public GearProxy(Gear data, IEnumerable<GearProxy> collection)
            : base(data, MyValidator.Instance)
        {
            _Collection = collection;
            ValidateAllProperties();
        }

        public int Id => WrappedElt.Id;

        public string Name
        {
            get => WrappedElt.Name;
            set => SetProperty(e => e.Name, value);
        }

        public GearType Type
        {
            get => WrappedElt.Type;
            set
            {
                if (SetProperty(e => e.Type, value))
                {   
                    // Clear size when changing type, to avoid invalid sizes
                    Size = string.Empty;
                }   
            }
        }

        public string BarCode
        {
            get => WrappedElt.BarCode;
            set => SetProperty(e => e.BarCode, value);
        }

        public string Size
        {
            get => WrappedElt.Size;
            set => SetProperty(e => e.Size, value);
        }

        private TimeSpan _StatsBorrowsDuration;
        public TimeSpan StatsBorrowsDuration
        {
            get => _StatsBorrowsDuration;
            private set => SetProperty(ref _StatsBorrowsDuration, value, false, false);
        }

        private int _StatsBorrowsCount;
        public int StatsBorrowsCount
        {
            get => _StatsBorrowsCount;
            private set => SetProperty(ref _StatsBorrowsCount , value, false, false);
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

        private class MyValidator : AbstractValidator<GearProxy>
        {
            private MyValidator()
            {
                RuleFor(e => e.Name).NotEmpty().WithMessage("Nom requis");
                RuleFor(e => e.Name).ItemUnique(gear=>gear._Collection, (gear1, gear2) => gear1.Name == gear2.Name && gear1.Type == gear2.Type).WithMessage("Le nom doit être unique pour un type d'équipement");

                RuleFor(e => e.BarCode).NotEmpty().WithMessage("Code barre requis");
                RuleFor(e => e.BarCode).ItemUnique(gear => gear._Collection).WithMessage("le code barre doit être unique");

                RuleFor(e=>e.Size).Must(size => string.IsNullOrEmpty(size) || AllowedTankSizes.Contains(size)).When(gear=>gear.Type == GearType.Tank).WithMessage("taille de bloc invalide");
                RuleFor(e => e.Size).Must(size => string.IsNullOrEmpty(size) || AllowedBCDSizes.Contains(size)).When(gear => gear.Type == GearType.BCD).WithMessage("taille de bloc invalide");

            }

            public static MyValidator Instance { get; } = new();
        }
    }

    /// <summary>
    /// ViewModel for the list of available Gears in the Settings view
    /// </summary>
    public class GearsListViewModel : GridHostViewModel<GearProxy>
    {
        public GearsListViewModel(IEnumerable<Gear> gears)
        {
            var proxies = gears.Select(g => BuildProxy(g));
            _Items.AddRange(proxies);
        }

        public void Add(Gear gear) => _Items.Add(BuildProxy(gear));

        public void Remove(GearProxy gear)
        {
            _Items.Remove(gear);
            ValidateAll();
        }

        override protected void OnCommitEdit() => ValidateAll();

        protected override void OnCancelEdit() => ValidateAll();

        private void ValidateAll()
        {
            foreach (var item in Items)
            {
                item.ValidateAllProperties();
            }
        }

        private GearProxy BuildProxy(Gear g) => new(g, Items);
    }
}
