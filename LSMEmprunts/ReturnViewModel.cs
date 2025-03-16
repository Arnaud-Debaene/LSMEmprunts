using DynamicData;
using DynamicData.Binding;
using LSMEmprunts.Data;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace LSMEmprunts
{
    public sealed class GearReturnInfo
    {
        public Gear Gear { get; set; }
        public bool Borrowed { get; set; }

        public string Name
        {
            get
            {
                var converter = new GearTypeToStringConverter();
                var converter2 = new GearDisplayNameConverter();
                return converter.Convert(Gear.Type, typeof(string), null, null) + " " + converter2.Convert(Gear, typeof(string), null, null);
            }
        }
    }

    public sealed class ReturnInfo : ReactiveObject
    {
        public Borrowing Borrowing { get; set; }
        public string Comment { get; set; }
    }

    public sealed class ReturnViewModel : ReactiveObject, IDisposable
    {
        private readonly Context _Context;

        public ObservableCollection<ReturnInfo> ClosingBorrowings { get; } = new();

        public ICollectionView Gears { get; }
  

        public ReturnViewModel()
        {
            SelectGearCommand = ReactiveCommand.Create<GearReturnInfo>(SelectGearCmd);
            
            var canValidateCmd = ClosingBorrowings.ToObservableChangeSet().ToCollection().Any();
            ValidateCommand = ReactiveCommand.Create(ValidateCmd, canValidateCmd);
            
            CancelCommand = ReactiveCommand.Create(GoBackToHomeView);

            this.WhenAnyValue(e => e.SelectedGearId).Subscribe(x => HandleSelectedGearIdChange(x));


            _Context = ContextFactory.OpenContext();

            var gears = (from gear in _Context.Gears
                         let borrowed = gear.Borrowings.Any(e => e.State == BorrowingState.Open)
                         select new GearReturnInfo { Gear = gear, Borrowed = borrowed }).OrderByDescending(e=>e.Borrowed)
                         .ToList();

            Gears = CollectionViewSource.GetDefaultView(gears);
            Gears.Filter = (item) =>
            {
                var gearInfo = (GearReturnInfo)item;
                if (ClosingBorrowings.Any(e=>e.Borrowing.Gear==gearInfo.Gear))
                {
                    return false;
                }
                return true;
            };
        }

        public void Dispose()
        {
            _AutoValidateTickerSubscription?.Dispose();
            _Context?.Dispose();
        }

        private string _SelectedGearId;
        public string SelectedGearId
        {
            get => _SelectedGearId;
            set => this.RaiseAndSetIfChanged(ref _SelectedGearId, value);
        }

        private void HandleSelectedGearIdChange(string selectedGearId)
        {
            if (string.IsNullOrEmpty(selectedGearId))
            {
                return;
            }

            var valueLower = selectedGearId.ToLower();
            var matchingGear = _Context.Gears.FirstOrDefault(e => e.Name.ToLower() == valueLower);
            if (matchingGear != null)
            {
                System.Diagnostics.Debug.WriteLine("Found a matching gear by name");
            }
            else
            {
                matchingGear = _Context.Gears.FirstOrDefault(e => e.BarCode == selectedGearId);
                if (matchingGear != null)
                {
                    System.Diagnostics.Debug.WriteLine("Found a matching gear by scan");
                }
            }

            if (matchingGear != null)
            {
                SelectGearToReturn(matchingGear);
            }
        }

        public ICommand ValidateCommand { get; }
        private void ValidateCmd()
        {
            _Context.SaveChanges();
            GoBackToHomeView();
        }

        public ICommand CancelCommand { get; }
        private void GoBackToHomeView()
        {
            MainWindowViewModel.Instance.CurrentPageViewModel = new HomeViewModel();
        }

        public ReactiveCommand<GearReturnInfo, Unit> SelectGearCommand { get; }
        public void SelectGearCmd(GearReturnInfo info)
        {
            SelectGearToReturn(info.Gear);
        }

        private IDisposable _AutoValidateTickerSubscription;

        private CountDownTicker _AutoValidateTicker;
        public CountDownTicker AutoValidateTicker
        {
            get => _AutoValidateTicker;
            set => this.RaiseAndSetIfChanged(ref _AutoValidateTicker, value);
        }

        private void SelectGearToReturn(Gear gear)
        {
            //check for double input of a given gear
            if (ClosingBorrowings.Any(e => e.Borrowing.Gear == gear))
            {
                var vm = new WarningWindowViewModel("Matériel déjà rendu");
                MainWindowViewModel.Instance.Dialogs.Add(vm);
                SelectedGearId = string.Empty;
                return;
            }

            var now = DateTime.Now;

            var matchingBorrowing = _Context.Borrowings.Include(e => e.Gear)
                .FirstOrDefault(e => e.Gear == gear && e.State == BorrowingState.Open);

            if (matchingBorrowing != null)
            {
                matchingBorrowing.ReturnTime = now;
                matchingBorrowing.State = BorrowingState.GearReturned;
                ClosingBorrowings.Add(new ReturnInfo { Borrowing = matchingBorrowing });
            }
            else
            {
                var msgVm = new WarningWindowViewModel("Matériel retourné sans avoir été emprunté");
                MainWindowViewModel.Instance.Dialogs.Add(msgVm);

                //create a "fake" borrowing with same Borrow / Return date
                var borrowing = new Borrowing
                {
                    BorrowTime = now,
                    ReturnTime = now,
                    Gear = gear,
                    State = BorrowingState.GearReturned,
                    Comment = "Retourné sans avoir été emprunté",
                };
                _Context.Borrowings.Add(borrowing);
                ClosingBorrowings.Add(new ReturnInfo { Borrowing = borrowing, Comment = "Retourné sans avoir été emprunté" });
            }

            if (AutoValidateTicker == null)
            {
                AutoValidateTicker = new CountDownTicker(60);
                _AutoValidateTickerSubscription = AutoValidateTicker.Tick.InvokeCommand(this, x => x.ValidateCommand);
            }
            else
            {
                AutoValidateTicker.Reset();
            }

            SelectedGearId = string.Empty;
            Gears.Refresh();
        }        
    }
}
