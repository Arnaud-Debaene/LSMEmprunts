using ReactiveUI;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace LSMEmprunts.Dialogs
{
    public partial class DialogManager : IDialogManager
    {
        #region SaveFileDialog
        public Interaction<SaveFileDialogViewModel, bool> SaveFile { get; } = new();

        private async Task HandleSaveFile(IInteractionContext<SaveFileDialogViewModel, bool> interaction)
        {
            var vm = interaction.Input;
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                AddExtension = vm.AddExtension,
                CreatePrompt = vm.CreatePrompt,
                Filter = vm.Filter,
                InitialDirectory = vm.InitialDirectory,
                OverwritePrompt = vm.OverwritePrompt,
                Title = vm.Title,
                ValidateNames = vm.ValidateNames,
                FileName = vm.FileName,
                
            };

            var result = (await ShowCommonDialogAsync(dialog)) == true;
            if (result)
            {
                vm.FileName = dialog.FileName;
            }
            interaction.SetOutput(result);
        }

        #endregion

        #region OpenFileDialog

        public Interaction<OpenFileDialogViewModel, bool> OpenFile { get; } = new();

        private async Task HandleOpenFile(IInteractionContext<OpenFileDialogViewModel, bool> interaction)
        {
            var vm = interaction.Input;
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = vm.Multiselect,
                ReadOnlyChecked = vm.ReadOnlyChecked,
                ShowReadOnly = vm.ShowReadOnly,
                FileName = vm.FileName,
                Filter = vm.Filter,
                InitialDirectory = vm.InitialDirectory,
                Title = vm.Title,
                ValidateNames = vm.ValidateNames
            };
            var result = (await ShowCommonDialogAsync(dialog)) == true;
            if (result)
            {
                vm.Multiselect = dialog.Multiselect;
                vm.ReadOnlyChecked = dialog.ReadOnlyChecked;
                vm.Filter = dialog.Filter;
                vm.FileName = dialog.FileName;
                vm.FileNames = dialog.FileNames;
                vm.SafeFileName = dialog.SafeFileName;
                vm.SafeFileNames = dialog.SafeFileNames;
            }
            interaction.SetOutput(result);
        }

        #endregion

        #region MessageBox

        public Interaction<MessageBoxViewModel, MessageBoxResult> MessageBox { get; } = new();

        private async Task HandleMessageBox(IInteractionContext<MessageBoxViewModel, MessageBoxResult> interaction)
        {
            var vm = interaction.Input;
            var topWindow = GetTopWIndow();
            var cts = new TaskCompletionSource<MessageBoxResult>();
            await topWindow.Dispatcher.BeginInvoke(new Action(() 
                => cts.SetResult(System.Windows.MessageBox.Show(topWindow,vm.Message, vm.Caption, vm.Buttons, vm.Image))));
            interaction.SetOutput(await cts.Task);
        }

        #endregion


    }
}
