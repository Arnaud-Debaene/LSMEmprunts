using ReactiveUI;
using Splat;
using System.Reactive.Disposables.Fluent;

namespace LSMEmprunts
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();

            // Resolve ViewModel from DI container or create directly
            ViewModel = AppLocator.Current.GetService<MainWindowViewModel>() ?? new MainWindowViewModel();

            this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel, x => x.Router, x => x.RoutedViewHost.Router).DisposeWith(disposables);
            });
        }
    }
}
