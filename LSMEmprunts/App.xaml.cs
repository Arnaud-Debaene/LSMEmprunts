using LSMEmprunts.Dialogs;
using ReactiveUI;
using ReactiveUI.Builder;
using Splat;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace LSMEmprunts
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");

            var rxUiInstance = RxAppBuilder.CreateReactiveUIBuilder()
                .WithWpf()
                .WithViewsFromAssembly(typeof(App).Assembly)
                .WithRegistration(locator =>
                {
                    // Register IScreen as a singleton so all resolutions share the same Router
                    locator.RegisterLazySingleton<IScreen>(() => new MainWindowViewModel());

                    locator.RegisterLazySingleton<IDialogManager>(() => new DialogManager(Locator.Current.GetService<IViewLocator>()));
                })
                .BuildApp();

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        
    }
}