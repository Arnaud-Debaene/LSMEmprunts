using DynamicData.Binding;
using LSMEmprunts.Data;
using LSMEmprunts.Dialogs;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Splat;
using System;
using System.Collections;
using System.Collections.Generic;
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
    public class GearBorrowInfo
    {
        public Gear Gear { get; set; }
        public bool Available { get; set; }

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

    /// <summary>
    /// comparer for ordering of gears list : by type then by name (if name is a number)
    /// </summary>
    class GearComparer : IComparer
    {
        private static GearType[] Types = { GearType.Tank, GearType.Regulator, GearType.BCD };

        public int Compare(object x, object y)
        {
            var xx = (GearBorrowInfo)x;
            var yy = (GearBorrowInfo)y;

            if (xx.Gear.Type != yy.Gear.Type)
            {
                return Array.IndexOf(Types, xx.Gear.Type) - Array.IndexOf(Types, yy.Gear.Type);
            }

            bool successParseNameX = int.TryParse(xx.Gear.Name, out var xxName);
            bool successParseNameY = int.TryParse(yy.Gear.Name, out var yyName);
            if (successParseNameX && successParseNameY)
            {
                return xxName - yyName;
            }
            else if (successParseNameX)
            {
                return -1;
            }
            else if (successParseNameY)
            {
                return 1;
            }
            else
            {
                return string.Compare(xx.Gear.Name, yy.Gear.Name);
            }
        }
    }

    public sealed partial class BorrowViewModel : ReactiveObject, IDisposable, IRoutableViewModel
    {
        public string UrlPathSegment => "borrow";
        public IScreen HostScreen { get; }

        private readonly Context _Context;

        private readonly ObservableCollection<User> _UsersList;

        public ICollectionView Users { get; }
        public ICollectionView Gears { get; }

        /// <summary>
        /// this property is bound to the current selection in the users listbox
        /// </summary>
        [Reactive]
        private User _CurrentUser;

        [ObservableAsProperty]
        private bool _UserSelected;

        private void SetSelectedUser(User user)
        {
            System.Diagnostics.Debug.Assert(user != null);

            CurrentUser = user;
            SelectedUserText = string.Empty;

            Users.Refresh();
            StartOrResetValidateTicker();
        }       

        public BorrowViewModel(IScreen screen)
        {
            HostScreen = screen;

            _Context = ContextFactory.OpenContext();

            var hasBorrowedGears = BorrowedGears.ToObservableChangeSet().Select(_ => BorrowedGears.Any());
            ValidateCommand = ReactiveCommand.CreateFromTask(async() =>
            {
                await SaveData();
                await GoBackToHomeViewAsync();
            }, hasBorrowedGears);
            CancelCommand = ReactiveCommand.CreateFromTask(GoBackToHomeViewAsync);
            SelectGearCommand = ReactiveCommand.CreateFromTask<GearBorrowInfo>(async(info) => await SelectGearToBorrowAsync(info.Gear));

            //configuration of UserSelected computed property
            _UserSelectedHelper = this.ObservableForProperty(x => x.CurrentUser, user => user != null).ToProperty(this, nameof(UserSelected));

            //configuration of GearTextBoxShallRetainFocus computed property
            _GearTextBoxShallRetainFocus = this.WhenAnyValue(x => x.CurrentUser, x => x.CommentTextBoxFocused, (user, commentSelected) => user != null && !commentSelected)
                .ToProperty(this, x => x.GearTextBoxShallRetainFocus);

            _UsersList = new ObservableCollection<User>(_Context.Users);
            Users = CollectionViewSource.GetDefaultView(_UsersList);
            Users.Filter = (item) =>
            {
                //filter users by name according to the text in the user selection text box
                if (string.IsNullOrEmpty(_SelectedUserText))
                {
                    return true;
                }
                return ((User)item).Name.StartsWith(_SelectedUserText, StringComparison.CurrentCultureIgnoreCase);
            };
            Users.SortDescriptions.Add(new SortDescription(nameof(User.Name), ListSortDirection.Ascending));

            //load all gears with an additional "Available" property to know if they are currently available for borrowing or not (not currently borrowed)
            var gears = (from gear in _Context.Gears
                         let borrowed = gear.Borrowings.Any(e => e.State == BorrowingState.Open)
                         select new GearBorrowInfo { Gear = gear, Available = !borrowed }).AsEnumerable()
                         .OrderByDescending(e => e.Available).ThenBy(e => e.Name).ToList();

            //create a collection view to be able to filter out gears that are in the process of being borrowed (in BorrowedGears)
            Gears = CollectionViewSource.GetDefaultView(gears);
            ((ListCollectionView)Gears).CustomSort = new GearComparer();
            Gears.Filter = (item) =>
            {
                //filter out gears that are already in the BorrowedGears list, so that a user cannot borrow the same gear twice
                var gearInfo = (GearBorrowInfo)item;
                if (BorrowedGears.Any(e => e == gearInfo.Gear))
                {
                    return false;
                }
                return true;
            };

            this.WhenAnyValue(e => e.SelectedUserText).Subscribe(x => HandleSelectedUserTextChange(x));

            //note: we use InvokeCommand here instead of Subscribe because ReactiveCommand disables the command while it is executing,
            //which avoids reentrancy issues if the user scans a gear while the previous scan is still being processed
            this.WhenAnyValue(e=>e.SelectedGearId).Throttle(TimeSpan.FromMilliseconds(300)).DistinctUntilChanged().Where(x=>!string.IsNullOrWhiteSpace(x))
                .ObserveOn(RxSchedulers.MainThreadScheduler).InvokeCommand(AnalyzeSelectedGearIdCommand);
        }

        public void Dispose()
        {
            _AutoValidateTickerSubscription?.Dispose();
            _Context.Dispose();
        }

        private async Task GoBackToHomeViewAsync()
        {
            Dispose();
            await HostScreen.Router.NavigateBack.Execute();
        }

        [Reactive]
        private string _SelectedUserText;


        /// <summary>
        /// Handle changes of the text in the user selection text box. 
        /// We try to find a single matching user by name and select it if found, so that the user needs only to type a few characters of his name to select himself.
        /// </summary>
        private void HandleSelectedUserTextChange(string value)
        {
            Users.Refresh();
            if (Users.Cast<User>().Count() == 1)
            {
                SetSelectedUser(Users.Cast<User>().First());
                return;
            }
        }

        private readonly List<Borrowing> _BorrowingsToForceClose = [];

        public ObservableCollection<Gear> BorrowedGears { get; } = [];

        [Reactive]
        private string _Comment;

        [Reactive]
        private string _SelectedGearId;

        /// <summary>
        /// Handle changes of the text in the selected gear text box. This text box is used to input either the name or the barcode of a gear,
        /// so that a user can quickly select a gear by scanning it or by typing its name.
        /// </summary>
        [ReactiveCommand]
        private async Task AnalyzeSelectedGearIdAsync(string value)
        {
            System.Diagnostics.Debug.WriteLine($"Analyzing selected gear id : {value}");
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            var valueLower = value.ToLower();
            var matchingGear = await _Context.Gears.FirstOrDefaultAsync(e => e.Name.ToLower() == valueLower);
            if (matchingGear == null)
            {
                matchingGear = await _Context.Gears.FirstOrDefaultAsync(e => e.BarCode == value);
            }

            if (matchingGear != null)
            {
                await SelectGearToBorrowAsync(matchingGear);
                SelectedGearId = string.Empty;
            }
        }

        public ICommand ValidateCommand { get; }
        private async Task SaveData()
        {
            _AutoValidateTickerSubscription?.Dispose();

            var date = DateTime.UtcNow;

            foreach (var existingBorrowing in _BorrowingsToForceClose)
            {
                existingBorrowing.State = BorrowingState.ForcedClose;
                existingBorrowing.Comment = existingBorrowing.Comment ?? string.Empty + " Clos de force car matériel réemprunté";
                existingBorrowing.ReturnTime = date;
            }

            await _Context.Borrowings.AddRangeAsync(BorrowedGears.Select(e =>
            new Borrowing
            {
                BorrowTime = date,
                Gear = e,
                User = CurrentUser,
                Comment = Comment,
                State = BorrowingState.Open
            }));
            await _Context.SaveChangesAsync();
            await _Context.Database.ExecuteSqlRawAsync($"NOTIFY {nameof(Borrowing)}");
        }

        public ICommand CancelCommand { get; }


        private IDisposable _AutoValidateTickerSubscription;

        [Reactive]
        private CountDownTicker _AutoValidateTicker;

        private void StartOrResetValidateTicker()
        {
            if (AutoValidateTicker == null && CurrentUser != null)
            {
                AutoValidateTicker = new CountDownTicker(120);
                _AutoValidateTickerSubscription = AutoValidateTicker.Tick.InvokeCommand(this, x => x.ValidateCommand);
            }
            else
            {
                AutoValidateTicker?.Reset();
            }
        }

        public ReactiveCommand<GearBorrowInfo, Unit> SelectGearCommand { get; }

        private async Task SelectGearToBorrowAsync(Gear gear)
        {
            //check for double input of a given gear
            if (BorrowedGears.Contains(gear))
            {
                var vm = new WarningWindowViewModel("Matériel déjà emprunté");
                await Locator.Current.GetService<IDialogManager>().WarningWindow.Handle(vm);
                return;
            }

            //try to find a still open borrowing of the same gear - force close it if found
            var existingBorrowing = await _Context.Borrowings.FirstOrDefaultAsync(e => e.GearId == gear.Id && e.State == BorrowingState.Open);
            if (existingBorrowing != null)
            {
                var confirmDlg = new ConfirmWindowViewModel("Ce matériel est déjà noté comme emprunté. L'emprunt en cours sera fermé. Etes vous sûr(e)?");
                if (await Locator.Current.GetService<IDialogManager>().ConfirmWindow.Handle(confirmDlg) == false)
                {
                    return;
                }
                _BorrowingsToForceClose.Add(existingBorrowing);
            }

            BorrowedGears.Add(gear);

            //start auto close ticker if required
            StartOrResetValidateTicker();

            Gears.Refresh();
        }

        #region keyboard focus handling
        private bool _CommentTextBoxFocused = false;
        /// <summary>
        /// property bound to the keyboard IsFocused property of the comment text box
        /// </summary>
        public bool CommentTextBoxFocused
        {
            get => _CommentTextBoxFocused;
            set => this.RaiseAndSetIfChanged(ref _CommentTextBoxFocused, value);

        }

        /// <summary>
        /// when this property is true, the Gear Id text box captures keyboard focus, so that all barcode scans go to it
        /// </summary>
        /// <remarks>
        /// We capture the keyboard:
        /// - once a user has been selected
        /// - and NOT when the user is inputing comment text
        /// </remarks>     
        private readonly ObservableAsPropertyHelper<bool> _GearTextBoxShallRetainFocus;
        public bool GearTextBoxShallRetainFocus => _GearTextBoxShallRetainFocus.Value;
        #endregion
    }
}