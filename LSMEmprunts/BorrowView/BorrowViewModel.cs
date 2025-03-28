using DynamicData.Binding;
using LSMEmprunts.Data;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
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

    public sealed class BorrowViewModel : ReactiveObject, IDisposable
    {
        private readonly Context _Context;

        private readonly ObservableCollection<User> _UsersList;

        public ICollectionView Users { get; }
        public ICollectionView Gears { get; }


        private User _CurrentUser;
        /// <summary>
        /// this property is bound to the current selection in the users listbox
        /// </summary>
        public User CurrentUser
        {
            get => _CurrentUser;
            set => this.RaiseAndSetIfChanged(ref _CurrentUser, value);
        }

        private readonly ObservableAsPropertyHelper<bool> _UserSelected;
        public bool UserSelected => _UserSelected.Value;

        private void SetSelectedUser(User user)
        {
            System.Diagnostics.Debug.Assert(user != null);

            CurrentUser = user;
            SelectedUserText = string.Empty;

            Users.Refresh();
            StartOrResetValidateTicker();
        }       

        public BorrowViewModel()
        {
            _Context = ContextFactory.OpenContext();

            var hasBorrowedGears = BorrowedGears.ToObservableChangeSet().Select(_ => BorrowedGears.Any());
            ValidateCommand = ReactiveCommand.Create(ValidateCmd, hasBorrowedGears);
            CancelCommand = ReactiveCommand.Create(GoBackToHomeView);
            SelectGearCommand = ReactiveCommand.Create<GearBorrowInfo>(SelectGearCmd);

            //configuration of UserSelected computed property
            _UserSelected = this.ObservableForProperty(x => x.CurrentUser, user => user != null).ToProperty(this, x => x.UserSelected);

            //configuration of GearTextBoxShallRetainFocus computed property
            _GearTextBoxShallRetainFocus = this.WhenAnyValue(x => x.CurrentUser, x => x.CommentTextBoxFocused, (user, commentSelected) => user != null && !commentSelected)
                .ToProperty(this, x => x.GearTextBoxShallRetainFocus);

            _UsersList = new ObservableCollection<User>(_Context.Users);
            Users = CollectionViewSource.GetDefaultView(_UsersList);
            Users.Filter = (item) =>
            {
                if (string.IsNullOrEmpty(_SelectedUserText))
                {
                    return true;
                }
                return ((User)item).Name.StartsWith(_SelectedUserText, StringComparison.CurrentCultureIgnoreCase);
            };
            Users.SortDescriptions.Add(new SortDescription(nameof(User.Name), ListSortDirection.Ascending));

            var gears = (from gear in _Context.Gears
                         let borrowed = gear.Borrowings.Any(e => e.State == BorrowingState.Open)
                         select new GearBorrowInfo { Gear = gear, Available = !borrowed }).OrderByDescending(e => e.Available)
                .ToList();

            Gears = CollectionViewSource.GetDefaultView(gears);
            ((ListCollectionView)Gears).CustomSort = new GearComparer();
            Gears.Filter = (item) =>
            {
                var gearInfo = (GearBorrowInfo)item;
                if (BorrowedGears.Any(e => e == gearInfo.Gear))
                {
                    return false;
                }
                return true;
            };

            this.WhenAnyValue(e => e.SelectedUserText).Subscribe(x => HandleSelectedUserTextChange(x));

            this.WhenAnyValue(e=>e.SelectedGearId).Subscribe(async (x) => await AnalyzeSelectedGearId(x));
        }

        public void Dispose()
        {
            _AutoValidateTickerSubscription?.Dispose();
            _Context.Dispose();
        }

        private string _SelectedUserText;
        public string SelectedUserText
        {
            get => _SelectedUserText;
            set => this.RaiseAndSetIfChanged(ref _SelectedUserText, value);           
        }

        private void HandleSelectedUserTextChange(string value)
        {
            Users.Refresh();
            if (Users.Cast<User>().Count() == 1)
            {
                System.Diagnostics.Debug.WriteLine("User input - found matching user by name");
                SetSelectedUser(Users.Cast<User>().First());
                return;
            }
        }

        private readonly List<Borrowing> _BorrowingsToForceClose = new List<Borrowing>();

        public ObservableCollection<Gear> BorrowedGears { get; } = new ObservableCollection<Gear>();

        private string _Comment;
        public string Comment
        {
            get => _Comment;
            set => this.RaiseAndSetIfChanged(ref _Comment, value);
        }

        private string _SelectedGearId;
        public string SelectedGearId
        {
            get => _SelectedGearId;
            set => this.RaiseAndSetIfChanged(ref _SelectedGearId, value);
        }

        private async Task AnalyzeSelectedGearId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            var valueLower = value.ToLower();
            var matchingGear = _Context.Gears.FirstOrDefault(e => e.Name.ToLower() == valueLower);

            if (matchingGear != null)
            {
                System.Diagnostics.Debug.WriteLine("Found matching gear by name");
            }
            else
            {
                matchingGear = _Context.Gears.FirstOrDefault(e => e.BarCode == value);
                if (matchingGear != null)
                {
                    System.Diagnostics.Debug.WriteLine("Found matching gear by scan");
                }
            }

            if (matchingGear != null)
            {
                await SelectGearToBorrow(matchingGear);
                SelectedGearId = string.Empty;
            }
        }

        public ICommand ValidateCommand { get; }
        private async void ValidateCmd()
        {
            _AutoValidateTickerSubscription?.Dispose();

            var date = DateTime.Now;

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

            GoBackToHomeView();
        }

        public ICommand CancelCommand { get; }
        private async void GoBackToHomeView()
        {
            await MainWindowViewModel.Instance.SetCurrentPage(new HomeViewModel());
        }

        private IDisposable _AutoValidateTickerSubscription;

        private CountDownTicker _AutoValidateTicker;
        public CountDownTicker AutoValidateTicker
        {
            get => _AutoValidateTicker;
            set => this.RaiseAndSetIfChanged(ref _AutoValidateTicker, value);
        }

        private void StartOrResetValidateTicker()
        {
            if (AutoValidateTicker == null && CurrentUser != null)
            {
                AutoValidateTicker = new CountDownTicker(60);
                _AutoValidateTickerSubscription = AutoValidateTicker.Tick.InvokeCommand(this, x => x.ValidateCommand);
            }
            else
            {
                AutoValidateTicker?.Reset();
            }
        }

        public ReactiveCommand<GearBorrowInfo, Unit> SelectGearCommand { get; }
        public async void SelectGearCmd(GearBorrowInfo info)
        {
            await SelectGearToBorrow(info.Gear);
        }

        private async Task SelectGearToBorrow(Gear gear)
        {
            //check for double input of a given gear
            if (BorrowedGears.Contains(gear))
            {
                var vm = new WarningWindowViewModel("Matériel déjà emprunté");
                MainWindowViewModel.Instance.Dialogs.Add(vm);
                return;
            }

            //try to find a still open borrowing of the same gear - force close it if found
            var existingBorrowing = _Context.Borrowings.FirstOrDefault(e => e.GearId == gear.Id && e.State == BorrowingState.Open);
            if (existingBorrowing != null)
            {
                var confirmDlg = new ConfirmWindowViewModel("Ce matériel est déjà noté comme emprunté. L'emprunt en cours sera fermé. Etes vous sûr(e)?");
                MainWindowViewModel.Instance.Dialogs.Add(confirmDlg);
                if (await confirmDlg.Result == false)
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