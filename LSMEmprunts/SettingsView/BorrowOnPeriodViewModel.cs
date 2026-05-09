using DynamicData.Binding;
using FluentValidation;
using LSMEmprunts.Data;
using LSMEmprunts.Dialogs;
using ReactiveUI;
using Splat;
using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LSMEmprunts
{
    public sealed record BorrowInfo(string User, GearType GearType, string Gear, DateTime FromDate, DateTime? ToDate);

    public class BorrowOnPeriodViewModel : ValidatableDlgViewModelBase<BorrowOnPeriodViewModel, Unit>
    {
        private readonly Context _Context;

        public BorrowOnPeriodViewModel(Context context)
            :base(MyValidator.Instance)
        {
            _Context = context;

            ExportCsvCommand = ReactiveCommand.Create(ExportCsv);

            var now = DateTime.UtcNow;
            _ToDateTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            _FromDateTime = ToDateTime - TimeSpan.FromDays(7);

            this.WhenAnyValue(x => x.FromDateTime, x => x.ToDateTime, x => x.InclusivePeriods)
                .Subscribe(((DateTime from, DateTime to, bool inclusive) t) => FillBorrows(t.from, t.to , t.inclusive));
        }

        private class MyValidator : AbstractValidator<BorrowOnPeriodViewModel>
        {
            private MyValidator()
            {
                RuleFor(x => x.FromDateTime).LessThan(vm => vm.ToDateTime).WithMessage("La date de début doit être inférieure à la date de fin");
                RuleFor(x => x.ToDateTime).GreaterThan(vm => vm.FromDateTime).WithMessage("La date de début doit être inférieure à la date de fin");
            }

            public static MyValidator Instance { get; } = new MyValidator();
        }

        #region Mvvm properties

        private DateTime _FromDateTime;

        public DateTime FromDateTime
        {
            get => _FromDateTime;
            set => this.RaiseAndSetIfChanged(ref _FromDateTime, value);
        }

        private DateTime _ToDateTime;

        public DateTime ToDateTime
        {
            get => _ToDateTime;
            set => this.RaiseAndSetIfChanged(ref _ToDateTime, value);
        }

        
        public ObservableCollectionExtended<BorrowInfo> Borrows { get; } = new();


        private bool _InclusivePeriods = false;
        public bool InclusivePeriods
        {
            get => _InclusivePeriods;
            set => this.RaiseAndSetIfChanged(ref _InclusivePeriods, value);
        }

        #endregion Mvvm properties

        private void FillBorrows(DateTime from, DateTime to, bool inclusive)
        {
            Borrows.Clear();
            if (from< to)
            {
                IQueryable<BorrowInfo> query;
                if (inclusive)
                {
                    query = _Context.Borrowings.Where(e => e.BorrowTime >= FromDateTime && e.ReturnTime <= ToDateTime).Select(e => new BorrowInfo(e.User.Name, e.Gear.Type, e.Gear.Name, e.BorrowTime, e.ReturnTime));
                }
                else
                {
                    query = _Context.Borrowings.Where(e =>
                    (e.BorrowTime < FromDateTime && e.ReturnTime > FromDateTime && e.ReturnTime <= ToDateTime) ||
                    (e.BorrowTime >= FromDateTime && e.BorrowTime < ToDateTime && e.ReturnTime > ToDateTime) ||
                    (e.BorrowTime >= FromDateTime && e.ReturnTime <= ToDateTime))
                        .Select(e => new BorrowInfo(e.User.Name, e.Gear.Type, e.Gear.Name, e.BorrowTime, e.ReturnTime));
                }

                Borrows.AddRange(query);
            }
        }

        #region Commands
        public ICommand ExportCsvCommand { get; }

        private async Task ExportCsv()
        {
            var vm = new SaveFileDialogViewModel
            {
                Filter = "(*.csv)|*.csv"
            };
            if (await Locator.Current.GetService<IDialogManager>().SaveFile.Handle(vm))
            {
                using var writer = new StreamWriter(vm.FileName, false, Encoding.UTF8);
                writer.WriteLine("Nom;Type;Matériel;Date d'emprunt;Date de retour");
                var converter = new GearTypeToStringConverter();
                foreach (var bi in Borrows)
                {
                    writer.WriteLine($"{bi.User};{converter.Convert(bi.GearType, typeof(string), null, Thread.CurrentThread.CurrentUICulture)};{bi.Gear};{bi.FromDate};{bi.ToDate};");
                }
            }
        }
        #endregion

    }
}