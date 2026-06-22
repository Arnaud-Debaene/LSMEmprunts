using DynamicData;
using DynamicData.Binding;
using LSMEmprunts.Data;
using LSMEmprunts.Dialogs;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Splat;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
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

    public sealed partial class ReturnViewModel : ReactiveObject, IDisposable, IRoutableViewModel
    {
        public string UrlPathSegment => "return";

        public IScreen HostScreen { get; }

        private readonly Context _Context;

        public ObservableCollection<ReturnInfo> ClosingBorrowings { get; } = [];

        public ICollectionView Gears { get; }


        public ReturnViewModel(IScreen screen)
        {
            HostScreen = screen;

            SelectGearCommand = ReactiveCommand.CreateFromTask<GearReturnInfo>(SelectGearCmdAsync);
            
            var canValidateCmd = ClosingBorrowings.ToObservableChangeSet().ToCollection().Any();
            ValidateCommand = ReactiveCommand.CreateFromTask(async() =>
            {
                await SaveData();
                await GoBackToHomeViewAsync();
            }, canValidateCmd);
            
            CancelCommand = ReactiveCommand.CreateFromTask(GoBackToHomeViewAsync);

            //note: we use InvokeCommand here instead of Subscribe because ReactiveCommand disables the command while it is executing,
            //which avoids reentrancy issues if the user scans a gear while the previous scan is still being processed
            this.WhenAnyValue(e => e.SelectedGearId).Throttle(TimeSpan.FromMilliseconds(300)).DistinctUntilChanged().Where(x => !string.IsNullOrWhiteSpace(x))
                .ObserveOn(RxSchedulers.MainThreadScheduler).InvokeCommand(HandleSelectedGearIdChangeCommand);

            _Context = ContextFactory.OpenContext();

            //load all gears with an additional "Borrowed" property to know if they are currently borrowed or not
            var gears = (from gear in _Context.Gears
                         let borrowed = gear.Borrowings.Any(e => e.State == BorrowingState.Open)
                         select new GearReturnInfo { Gear = gear, Borrowed = borrowed }).AsEnumerable()
                         .OrderByDescending(e=>e.Borrowed).ThenBy(e=>e.Name).ToList();

            //create a collection view to be able to filter out gears that are in the process of being returned (in ClosingBorrowings)
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

        private async Task GoBackToHomeViewAsync()
        {
            Dispose();
            await HostScreen.Router.NavigateBack.Execute();
        }

        [Reactive]
        private string _SelectedGearId;

        [ReactiveCommand]
        private async Task HandleSelectedGearIdChangeAsync(string selectedGearId)
        {
            if (string.IsNullOrEmpty(selectedGearId))
            {
                return;
            }

            var valueLower = selectedGearId.ToLower();
            var matchingGear = await _Context.Gears.FirstOrDefaultAsync(e => e.Name.ToLower() == valueLower);
            if (matchingGear == null)            
            {
                matchingGear = await _Context.Gears.FirstOrDefaultAsync(e => e.BarCode == selectedGearId);
            }

            if (matchingGear != null)
            {
                await SelectGearToReturn(matchingGear);
            }
        }

        public ICommand ValidateCommand { get; }
        private async Task SaveData()
        {
            await _Context.SaveChangesAsync();
            await _Context.Database.ExecuteSqlRawAsync($"NOTIFY {nameof(Borrowing)}");
        }

        public ICommand CancelCommand { get; }
     

        public ReactiveCommand<GearReturnInfo, Unit> SelectGearCommand { get; }
        public async Task SelectGearCmdAsync(GearReturnInfo info)
        {
            await SelectGearToReturn(info.Gear);
        }

        private IDisposable _AutoValidateTickerSubscription;

        [Reactive]
        private CountDownTicker _AutoValidateTicker;       

        private async Task SelectGearToReturn(Gear gear)
        {
            //check for double input of a given gear
            if (ClosingBorrowings.Any(e => e.Borrowing.Gear == gear))
            {
                var vm = new WarningWindowViewModel("Matériel déjà rendu");
                await Locator.Current.GetService<IDialogManager>().WarningWindow.Handle(vm);
                SelectedGearId = string.Empty;
                return;
            }

            var now = DateTime.UtcNow;

            var matchingBorrowing = await _Context.Borrowings.Include(e => e.Gear)
                .FirstOrDefaultAsync(e => e.Gear == gear && e.State == BorrowingState.Open);

            if (matchingBorrowing != null)
            {
                matchingBorrowing.ReturnTime = now;
                matchingBorrowing.State = BorrowingState.GearReturned;
                ClosingBorrowings.Add(new ReturnInfo { Borrowing = matchingBorrowing });
            }
            else
            {
                var msgVm = new WarningWindowViewModel("Matériel retourné sans avoir été emprunté");
                await Locator.Current.GetService<IDialogManager>().WarningWindow.Handle(msgVm);
                
                //create a "fake" borrowing with same Borrow / Return date
                var borrowing = new Borrowing
                {
                    BorrowTime = now,
                    ReturnTime = now,
                    Gear = gear,
                    State = BorrowingState.GearReturned,
                    Comment = "Retourné sans avoir été emprunté",
                };
                await _Context.Borrowings.AddAsync(borrowing);
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
