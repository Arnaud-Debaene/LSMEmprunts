using LSMEmprunts.Behaviors;
using ReactiveUI;
using System.Reactive.Disposables.Fluent;

namespace LSMEmprunts
{
    /// <summary>
    /// Interaction logic for GearIdEditor.xaml
    /// </summary>
    public partial class GearIdEditor : ReactiveUserControl<GearProxy>
    {
        public GearIdEditor()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                this.Bind(ViewModel, x => x.BarCode, x => x.TxtBox.Text).DisposeWith(disposables);
                TxtBox.ConfigureRfidInput(disposables);
            });
        }
    }
}
