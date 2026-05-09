using Microsoft.Win32;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace LSMEmprunts.Dialogs
{
    public partial class DialogManager : IDialogManager
    {
        private readonly IViewLocator _viewLocator;
        private readonly Stack<Window> _modalStack = new();

        public DialogManager(IViewLocator viewLocator)
        {
            _viewLocator = viewLocator;

            SaveFile.RegisterHandler(HandleSaveFile);
            OpenFile.RegisterHandler(HandleOpenFile);
            MessageBox.RegisterHandler(HandleMessageBox);

            RegisterInteraction(ConfirmWindow);
        }

        public Interaction<ConfirmWindowViewModel, bool> ConfirmWindow { get; } = new();

        public void RegisterInteraction<TViewModel, TResult>(IInteraction<TViewModel, TResult> interaction) where TViewModel : ModalDialogViewModelBase<TResult>
        {
            interaction.RegisterHandler(async ctx =>
            {
                var vm = ctx.Input;

                var view = _viewLocator.ResolveView(vm);
                view.ViewModel = vm;

                var topWindow = GetTopWIndow();

                var window = new Window
                {
                    Content = view,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    ShowInTaskbar = false,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStyle = WindowStyle.None,
                    Owner = topWindow
                };
                _modalStack.Push(window);

                vm.CloseCommand.Subscribe(_ =>
                {
                    window.Close();
                });

                window.Closed += (_, _) =>
                {
                    if (_modalStack.Peek() == window)
                        _modalStack.Pop();

                    if (!vm.ResultTaskSource.Task.IsCompleted)
                        vm.ResultTaskSource.TrySetCanceled();
                };

                //note: we do not block on the ShowDialog call to allow for recursive modal dialogs to be displayed
                await topWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    window.ShowDialog();
                }));

                ctx.SetOutput(await vm.ResultTaskSource.Task);
            });
        }

        private async Task<bool?> ShowCommonDialogAsync(CommonDialog dlg)
        {
            var window = GetTopWIndow();
            var cts = new TaskCompletionSource<bool?>();
            await window.Dispatcher.BeginInvoke(new Action(() => cts.SetResult(dlg.ShowDialog(window))));
            return await cts.Task;
        }

        private Window GetTopWIndow() => _modalStack.Count > 0 ? _modalStack.Peek() : Application.Current.MainWindow;
    }
}
