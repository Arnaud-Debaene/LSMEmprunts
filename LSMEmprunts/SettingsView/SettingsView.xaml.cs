using LSMEmprunts.Dialogs;
using ReactiveUI;
using Splat;
using System;
using System.Globalization;
using System.Reactive.Disposables.Fluent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LSMEmprunts
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : ReactiveUserControl<SettingsViewModel>
    {
        public SettingsView()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                DataContext = ViewModel;

                this.BindCommand(ViewModel, x => x.ValidateCommand, x => x.ValidateBtn).DisposeWith(disposables);

                this.BindCommand(ViewModel, x => x.CancelCommand, x => x.CancelBtn).DisposeWith(disposables);

                this.BindCommand(ViewModel, x => x.ShowBorrowOnPeriodCommand, x => x.ShowBorrowOnPeriodBtn).DisposeWith(disposables);

                this.BindCommand(ViewModel, x => x.CreateUserCommand, x => x.CreateUserBtn).DisposeWith(disposables);
                this.BindCommand(ViewModel, x => x.UsersCsvCommand, x => x.UsersCsvBtn).DisposeWith(disposables);

                this.BindCommand(ViewModel, x => x.CreateGearCommand, x => x.CreateGearBtn).DisposeWith(disposables);
                this.BindCommand(ViewModel, x => x.GearsCsvCommand, x => x.GearsCsvBtn).DisposeWith(disposables);

                UsersGrid.DataContext = ViewModel.Users;
                this.Bind(ViewModel, x => x.Users.Items, x => x.UsersGrid.ItemsSource).DisposeWith(disposables);

                GearsGrid.DataContext = ViewModel.Gears;
                this.Bind(ViewModel, x => x.Gears.Items, x => x.GearsGrid.ItemsSource).DisposeWith(disposables);

                this.Bind(ViewModel, x=>x.StatisticsStartDate, x=>x.StatisticsStartDatePicker.SelectedDate).DisposeWith(disposables);

                var dialogManager = Locator.Current.GetService<IDialogManager>();
                dialogManager.RegisterInteraction(ViewModel.ShowUserHistoryDialog);
                dialogManager.RegisterInteraction(ViewModel.ShowGearHistoryDialog);
                dialogManager.RegisterInteraction(ViewModel.ShowBorrowOnPeriodDialog);
            });
        }
    }

    public class GearSizeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TankSizeTemplate { get; set; }

        public DataTemplate BCDSizeTemplate { get; set; }

        public DataTemplate EmptyTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
            {
                return null;
            }

            var proxy = (GearProxy)item;
            switch (proxy.Type)
            {
                case Data.GearType.Tank:
                    return TankSizeTemplate;

                case Data.GearType.BCD:
                    return BCDSizeTemplate;

                default:
                    return EmptyTemplate;
            };
        }
    }

    public class DurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan ts = (TimeSpan)value;
            if (ts==TimeSpan.Zero)
            {
                return "0";
            }
            if (ts.Days==0)
            {
                return string.Format("{0:%h} heures {0:%m} mins", ts);
            }
            return string.Format("{0:%d} jours {0:%h} heures", ts);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}