using System;
using DynamicData.Binding;
using LSMEmprunts.Data;
using MvvmDialogs;
using ReactiveUI;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using FluentValidation;

namespace LSMEmprunts
{
    public sealed class BorrowInfo
    {
        public string User { get; set; }
        public GearType GearType { get; set; }
        public string Gear { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class BorrowOnPeriodViewModel : ValidatableDlgViewModelBase<BorrowOnPeriodViewModel>
    {
        private readonly Context _Context;

        public BorrowOnPeriodViewModel(Context context)
            :base(MyValidator.Instance)
        {
            _Context = context;

            ExportCsvCommand = ReactiveCommand.Create(ExportCsv);

            var now = DateTime.Now;
            _ToDateTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Local);
            _FromDateTime = ToDateTime - TimeSpan.FromDays(7);

            this.WhenAnyValue(x => x.FromDateTime, x => x.ToDateTime, x => x.InclusivePeriods)
                .Subscribe(((DateTime from, DateTime to, bool inclusive) t) => FillBorrows(t.from, t.to , t.inclusive));
        }

        private class MyValidator : FluentWpfValidator<BorrowOnPeriodViewModel>
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
            System.Diagnostics.Debug.WriteLine("FillBorrows");
            Borrows.Clear();
            if (from< to)
            {
                IQueryable<BorrowInfo> query;
                if (inclusive)
                {
                    query = _Context.Borrowings.Where(e => e.BorrowTime >= FromDateTime && e.ReturnTime <= ToDateTime).Select(e => new BorrowInfo
                    {
                        FromDate = e.BorrowTime,
                        ToDate = e.ReturnTime,
                        User = e.User.Name,
                        Gear = e.Gear.Name,
                        GearType = e.Gear.Type,
                    });
                }
                else
                {
                    query = _Context.Borrowings.Where(e =>
                    (e.BorrowTime < FromDateTime && e.ReturnTime > FromDateTime && e.ReturnTime <= ToDateTime) ||
                    (e.BorrowTime >= FromDateTime && e.BorrowTime < ToDateTime && e.ReturnTime > ToDateTime) ||
                    (e.BorrowTime >= FromDateTime && e.ReturnTime <= ToDateTime))
                        .Select(e => new BorrowInfo
                        {
                            FromDate = e.BorrowTime,
                            ToDate = e.ReturnTime,
                            User = e.User.Name,
                            Gear = e.Gear.Name,
                            GearType = e.Gear.Type,
                        });
                }

                Borrows.AddRange(query);
            }
        }

        #region Commands
        public ICommand ExportCsvCommand { get; }

        private async void ExportCsv()
        {
            var vm = new SaveFileDialogViewModel
            {
                Filter = "(*.csv)|*.csv"
            };
            MainWindowViewModel.Instance.Dialogs.Add(vm);
            if (await vm.Completion)
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