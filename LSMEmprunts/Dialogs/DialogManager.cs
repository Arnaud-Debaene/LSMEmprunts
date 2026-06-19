using Microsoft.Win32;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows;

namespace LSMEmprunts.Dialogs
{
    /// <summary>
    /// Manages the display and interaction of modal dialogs and common file/message dialogs in the application.
    /// Implements a reactive-based dialog system using ReactiveUI interactions, supporting both custom modal dialogs
    /// and standard Windows dialogs (file dialogs, message boxes). Supports nested/recursive modal dialogs.
    /// </summary>
    public partial class DialogManager : IDialogManager
    {
        private readonly IViewLocator _viewLocator;
        private readonly Stack<Window> _modalStack = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogManager"/> class.
        /// </summary>
        /// <param name="viewLocator">The view locator service used to resolve views for view models.</param>
        public DialogManager(IViewLocator viewLocator)
        {
            _viewLocator = viewLocator;

            SaveFile.RegisterHandler(HandleSaveFile);
            OpenFile.RegisterHandler(HandleOpenFile);
            MessageBox.RegisterHandler(HandleMessageBox);

            RegisterInteraction(ConfirmWindow);
            RegisterInteraction(WarningWindow);
        }

        /// <summary>
        /// Gets the interaction for displaying a confirmation dialog.
        /// </summary>
        public Interaction<ConfirmWindowViewModel, bool> ConfirmWindow { get; } = new();

        /// <summary>
        /// Gets the interaction for displaying a warning dialog.
        /// </summary>
        public Interaction<WarningWindowViewModel, Unit> WarningWindow { get; } = new();

        /// <summary>
        /// Registers a handler for a custom modal dialog interaction.
        /// Creates a modal window with the provided view model and manages its lifecycle,
        /// including support for nested modal dialogs.
        /// </summary>
        /// <typeparam name="TViewModel">The type of the view model for the dialog, must derive from <see cref="ModalDialogViewModelBase{TResult}"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result returned by the dialog.</typeparam>
        /// <param name="interaction">The interaction to register the handler for.</param>
        public void RegisterInteraction<TViewModel, TResult>(IInteraction<TViewModel, TResult> interaction) where TViewModel : ModalDialogViewModelBase<TResult>
        {
            interaction.RegisterHandler(async ctx =>
            {
                // Extract the view model from the interaction context
                var vm = ctx.Input;

                // Resolve the appropriate view for this view model using the view locator service
                // and bind the view model to the view
                var view = _viewLocator.ResolveView(vm);
                view.ViewModel = vm;

                // Get the current top-level window (either the topmost modal or the main window)
                var topWindow = GetTopWIndow();

                // Create a new modal window with the resolved view as its content
                // Configure window properties for a frameless, non-resizable modal dialog
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
                // Add the new window to the modal stack to track the dialog hierarchy
                _modalStack.Push(window);

                // Subscribe to the view model's close command to close the window when the dialog is dismissed
                vm.CloseCommand.Subscribe(_ =>
                {
                    window.Close();
                });

                // Handle cleanup when the window is closed
                window.Closed += (_, _) =>
                {
                    // Remove the window from the modal stack if it's the top window
                    if (_modalStack.Peek() == window)
                        _modalStack.Pop();

                    // If the result task has not been completed, mark it as cancelled
                    // This handles the case where the window is closed by the user without a result being set
                    if (!vm.ResultTaskSource.Task.IsCompleted)
                        vm.ResultTaskSource.TrySetCanceled();
                };

                // Display the modal dialog on the UI thread without blocking, allowing nested/recursive modal dialogs
                // We use BeginInvoke instead of ShowDialog to avoid blocking the UI thread
                await topWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    window.ShowDialog();
                }));

                // Return the dialog result to the interaction context once the dialog completes
                ctx.SetOutput(await vm.ResultTaskSource.Task);
            });
        }

        /// <summary>
        /// Shows a common dialog (file or message box) asynchronously on the UI thread.
        /// </summary>
        /// <param name="dlg">The common dialog to display.</param>
        /// <returns>A task representing the asynchronous operation, with a result indicating the dialog outcome.</returns>
        private async Task<bool?> ShowCommonDialogAsync(CommonDialog dlg)
        {
            var window = GetTopWIndow();
            var cts = new TaskCompletionSource<bool?>();
            await window.Dispatcher.BeginInvoke(new Action(() => cts.SetResult(dlg.ShowDialog(window))));
            return await cts.Task;
        }

        /// <summary>
        /// Gets the topmost window, prioritizing modal windows in the stack or falling back to the main application window.
        /// </summary>
        /// <returns>The topmost window, or the main application window if no modal windows are active.</returns>
        private Window GetTopWIndow() => _modalStack.Count > 0 ? _modalStack.Peek() : Application.Current.MainWindow;
    }
}
