using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LSMEmprunts
{
    /// <summary>
    /// A WPF user control that provides inline cell editing functionality for GridViews.
    /// Manages the display and editing states of a cell, handling transitions between display and edit modes
    /// with keyboard support (Enter to commit, Escape to cancel).
    /// </summary>
    public partial class EditableCell : ReactiveUserControl<IReactiveObject>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EditableCell"/> class.
        /// Sets up reactive bindings to observe editing state changes and manages visibility of display and editor presenters.
        /// </summary>
        public EditableCell()
        {
            InitializeComponent();

            _HandleClickOutsideControlHandler = new MouseButtonEventHandler(HandleMouseClickOutsideControl);

            PART_EditorPresenter.IsVisibleChanged += OnPartEditorVisibleChanged;

            this.WhenActivated(disposables =>
            {
                var hostEditingCellObs = this.WhenAnyValue(x => x.Host.EditingCell);
                var rowObs = this.WhenAnyValue(x => x.DataContext).Where(x => x != null);
                var colObs = this.WhenAnyValue(x => x.ColumnKey).Where(x => !string.IsNullOrWhiteSpace(x));
                //compute when the cell is being edited by comparing the host's EditingCell with the current cell coordinates (row and column)
                var isEditingObs = hostEditingCellObs.CombineLatest(rowObs, colObs, (editingCell, row, col) =>
                {
                    return editingCell.HasValue && ReferenceEquals(editingCell.Value.RowVm, row) && StringComparer.Ordinal.Equals(editingCell.Value.ColumnKey, col);
                }).DistinctUntilChanged();

                isEditingObs.ObserveOn(RxSchedulers.MainThreadScheduler).Subscribe(isEditing =>
                {
                    //set visibility of the display and editor presenters based on the editing state
                    PART_DisplayPresenter.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
                    PART_EditorPresenter.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;

                    if (isEditing)
                    {
                        // when becoming visible, set focus to the editor presenter
                        PART_EditorPresenter.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            PART_EditorPresenter.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                        }), System.Windows.Threading.DispatcherPriority.Background);

                        if (ClickOutsideEndEndition)
                        {
                            //add a mouse preview handler to the top window to detect when the mouse is clicked outside the control.
                            //When that happens, commit the edit
                            Window.GetWindow(this).AddHandler(Mouse.PreviewMouseDownEvent, _HandleClickOutsideControlHandler, true);
                        }

                    }
                }).DisposeWith(disposables);
               
            });
        }

        #region handle click outside of EditableCell to commit edit

        private readonly MouseButtonEventHandler _HandleClickOutsideControlHandler;

        /// <summary>
        /// Handler for PreviewMouseDownEvent when the control is in edit mode.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void HandleMouseClickOutsideControl(object source, MouseButtonEventArgs e)
        {
            var mousePosition = e.GetPosition(this);
            if (mousePosition.X < 0 || mousePosition.Y < 0 || mousePosition.X > RenderSize.Width || mousePosition.Y > RenderSize.Height)
            {
                //click outside of the control --> commit the edit then remove the PreviewMouseDownEvent subscribtion
                Host.CommitEdit.Execute().Subscribe();
                Window.GetWindow(this). RemoveHandler(Mouse.PreviewMouseDownEvent, _HandleClickOutsideControlHandler);
            }
        }

        #region ClickOutsideEndEndition dependency property
        public static readonly DependencyProperty ClickOutsideEndEnditionProperty =
            DependencyProperty.Register(nameof(ClickOutsideEndEndition), typeof(bool), typeof(EditableCell), new PropertyMetadata(true));

        public bool ClickOutsideEndEndition
        {
            get => (bool)GetValue(ClickOutsideEndEnditionProperty);
            set => SetValue(ClickOutsideEndEnditionProperty, value);
        }
        #endregion

        #endregion

        #region ColumnKey Dependency Property
        public static readonly DependencyProperty ColumnKeyProperty =
            DependencyProperty.Register(nameof(ColumnKey), typeof(string), typeof(EditableCell), new PropertyMetadata(default(string)));
        public string ColumnKey
        {
            get => (string)GetValue(ColumnKeyProperty);
            set => SetValue(ColumnKeyProperty, value);
        }
        #endregion

        #region EditorTemplate Dependency Property
        public static readonly DependencyProperty EditorTemplateProperty =
            DependencyProperty.Register(nameof(EditorTemplate), typeof(DataTemplate), typeof(EditableCell), new PropertyMetadata(default(DataTemplate)));
        public DataTemplate EditorTemplate
        {
            get => (DataTemplate)GetValue(EditorTemplateProperty);
            set => SetValue(EditorTemplateProperty, value);
        }
        #endregion

        #region EditorTemplateSelector Dependendcy Property
        public static readonly DependencyProperty EditorTemplateSelectorProperty =
            DependencyProperty.Register(nameof(EditorTemplateSelector), typeof(DataTemplateSelector), typeof(EditableCell), new PropertyMetadata(default(DataTemplateSelector)));
        public DataTemplateSelector EditorTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(EditorTemplateSelectorProperty);
            set => SetValue(EditorTemplateSelectorProperty, value);
        }
        #endregion

        #region DisplayContent Dependency Property
        public static readonly DependencyProperty DisplayContentProperty =
            DependencyProperty.Register(nameof(DisplayContent), typeof(object), typeof(EditableCell), new PropertyMetadata(default(object)));
        public object DisplayContent
        {
            get => GetValue(DisplayContentProperty);
            set => SetValue(DisplayContentProperty, value);
        }
        #endregion

        #region Host property

        /// <summary>
        /// Gets the <see cref="ICellEditHost"/> that manages edit operations for this cell.
        /// Searches the visual tree up from this control to find the first ancestor with a DataContext implementing <see cref="ICellEditHost"/>.
        /// </summary>
        public ICellEditHost Host => FindHost();

        /// <summary>
        /// Searches the visual tree upward to find the first <see cref="FrameworkElement"/> 
        /// whose DataContext implements <see cref="ICellEditHost"/>.
        /// </summary>
        /// <returns>The <see cref="ICellEditHost"/> from the ancestor's DataContext, or null if not found.</returns>
        private ICellEditHost FindHost()
        {
            DependencyObject current = this;
            while (current != null)
            {
                current = VisualTreeHelper.GetParent(current);
                if (current is FrameworkElement fe && fe.DataContext is ICellEditHost host)
                    return host;
            }
            return null;
        }

        #endregion

        #region click handler
        /// <summary>
        /// Handles double-click events to initiate cell editing.
        /// Requests the host to begin editing this cell, or does nothing if already in edit mode.
        /// </summary>
        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var host = Host;
            var row = DataContext;
            var col = ColumnKey;
            if (host==null || row==null || string.IsNullOrWhiteSpace(col))
            {
                return;
            }

            if (host.EditingCell.HasValue && ReferenceEquals(host.EditingCell.Value.RowVm, row) && StringComparer.Ordinal.Equals(host.EditingCell.Value.ColumnKey, col))
            {
                //already editing this cell, do nothing
                e.Handled = true;
                return;
            }

            // ask the host to begin editing this cell
            host.BeginEditCell.Execute(new CellId(row, col)).Subscribe();
            e.Handled = true;
        }
        #endregion

        #region Handling Enter and Escape keys to commit/cancel edit
        /// <summary>
        /// Handles visibility changes of the editor presenter to set up keyboard bindings.
        /// When the editor becomes visible, adds key bindings (Enter for commit, Escape for cancel) 
        /// to the editor control after the visual tree is updated.
        /// </summary>
        private void OnPartEditorVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //we need to wait for the editor to be visble AND its visual tree updated before we can add the Key Bindings to its child control
            if (PART_EditorPresenter.Visibility == Visibility.Visible)
            {
                /*run the following code asynchronously with a low priority, after the current event handler,
                 * to let WPF finish its processing of the visibility change and the visual tree update that comes with it (that is, the editor control being added as a child of PART_EditorPresenter),
                 * so that we can find the editor control in the visual tree and set key bindings on it*/
                PART_EditorPresenter.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (VisualTreeHelper.GetChildrenCount(PART_EditorPresenter) == 1)
                    {
                        var editorControl = VisualTreeHelper.GetChild(PART_EditorPresenter, 0) as Control;
                        if (editorControl != null)
                        {
                            editorControl.InputBindings.Add(new KeyBinding(Host.CommitEdit, new KeyGesture(Key.Enter)));
                            editorControl.InputBindings.Add(new KeyBinding(Host.CancelEdit, new KeyGesture(Key.Escape)));
                        }
                        //once key bindings are set, we are not interested anymore in this event, so we can unhook it to avoid unnecessary future calls
                        PART_EditorPresenter.IsVisibleChanged -= OnPartEditorVisibleChanged;
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
        #endregion
    }
}
