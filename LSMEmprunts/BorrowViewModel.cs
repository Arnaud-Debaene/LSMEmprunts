using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSMEmprunts.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

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

    public sealed class BorrowViewModel : ObservableObject, IDisposable
    {
        private readonly Context _Context;

        private readonly List<User> _UsersList;

        public ICollectionView Users { get; }
        public ICollectionView Gears { get; }


        private User _CurrentUser;
        /// <summary>
        /// this property is bound to the current selection in the users listbox
        /// </summary>
        public User CurrentUser
        {
            get => _CurrentUser;
            set
            {
                if (SetProperty(ref _CurrentUser, value) && value!=null)
                {
                    SetSelectedUser(value);
                }
            }
        }

        private void SetSelectedUser(User user)
        {
            System.Diagnostics.Debug.Assert(user != null);
            SelectedUsers.Clear();
            SelectedUsers.Add(user);

            CurrentUser = user;
            _SelectedUserText = null;

            OnPropertyChanged(nameof(SelectedUserText));
            OnPropertyChanged(nameof(UserSelected));
            OnPropertyChanged(nameof(SelectedUser));

            Users.Refresh();
            StartOrResetValidateTicker();
            GearInputFocused = true; //move focus to gear input.            
        }

        public bool UserSelected => SelectedUsers.Count > 0;

        public User SelectedUser => SelectedUsers.FirstOrDefault();

        /// <summary>
        /// this collection contains at most 1 item that is the "curerntly selected and validated" user : displayed in bold below the users list, 
        /// and the one that is used when saving the borrow operation
        /// </summary>
        public ObservableCollection<User> SelectedUsers { get; } = new ObservableCollection<User>();

        public BorrowViewModel()
        {
            _Context = ContextFactory.OpenContext();

            ValidateCommand = new RelayCommand(ValidateCmd, CanValidateCmd);
            CancelCommand = new RelayCommand(GoBackToHomeView);
            SelectGearCommand = new RelayCommand<GearBorrowInfo>(SelectGearCmd);

            _UsersList = _Context.Users.ToList();
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
                var gearInfo = (GearBorrowInfo) item;
                if (BorrowedGears.Any(e => e == gearInfo.Gear))
                {
                    return false;
                }
                return true;
            };
        }

        public void Dispose()
        {
            AutoValidateTicker?.Dispose();
            _Context.Dispose();
        }

        private string _SelectedUserText;
        public string SelectedUserText
        {
            get => _SelectedUserText;
            set
            {
                _SelectedUserText = value;
                Users.Refresh();

                if (Users.Cast<User>().Count() == 1)
                {
                    System.Diagnostics.Debug.WriteLine("User input - found matching user by name");
                   SetSelectedUser(Users.Cast<User>().First());
                    return;
                }

                OnPropertyChanged();
            }
        }

        private readonly List<Borrowing> _BorrowingsToForceClose = new List<Borrowing>();

        public ObservableCollection<Gear> BorrowedGears { get; } = new ObservableCollection<Gear>();

        private string _Comment;
        public string Comment
        {
            get => _Comment;
            set => SetProperty(ref _Comment, value);
        }

        private string _SelectedGearId;
        public string SelectedGearId
        {
            get => _SelectedGearId;
            set
            {
                if (SetProperty(ref _SelectedGearId, value))
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => AnalyzeSelectedGearId(value)));
                }
            }
        }

        private async void AnalyzeSelectedGearId(string value)
        {
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
                SetProperty(ref _SelectedGearId, string.Empty, nameof(SelectedGearId));
            }
        }

        public RelayCommand ValidateCommand { get; }
        private async void ValidateCmd()
        {
            AutoValidateTicker?.Dispose();

            var date = DateTime.Now;

            foreach(var existingBorrowing in _BorrowingsToForceClose)
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
                User = SelectedUser,
                Comment = Comment,
                State = BorrowingState.Open
            }));
            await _Context.SaveChangesAsync();

            GoBackToHomeView();
        }
        private bool CanValidateCmd()
        {
            return SelectedUser != null && BorrowedGears.Any();
        }

        public RelayCommand CancelCommand { get; }
        private void GoBackToHomeView()
        {
            MainWindowViewModel.Instance.CurrentPageViewModel = new HomeViewModel();
        }

        private CountDownTicker _AutoValidateTicker;
        public CountDownTicker AutoValidateTicker{
            get => _AutoValidateTicker;
            set => SetProperty(ref _AutoValidateTicker, value);
        }

        private void StartOrResetValidateTicker()
        {
            if (AutoValidateTicker == null && SelectedUser != null)
            {
                AutoValidateTicker = new CountDownTicker(60);
                AutoValidateTicker.Tick += () =>
                {
                    if (CanValidateCmd())
                        ValidateCmd();
                };
            }
            else
            {
                AutoValidateTicker?.Reset();
            }
        }

        private bool _UserInputFocused = true;
        public bool UserInputFocused
        {
            get => _UserInputFocused;
            set => SetProperty(ref _UserInputFocused, value);
        }

        private bool _GearInputFocused;
        public bool GearInputFocused
        {
            get => _GearInputFocused;
            set => SetProperty(ref _GearInputFocused, value);
        }

        public RelayCommand<GearBorrowInfo> SelectGearCommand { get; }
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

            ValidateCommand.NotifyCanExecuteChanged();

            //start auto close ticker if required
            StartOrResetValidateTicker();

            Gears.Refresh();
            GearInputFocused = true; //move focus to gear input.
        }
    }
}
