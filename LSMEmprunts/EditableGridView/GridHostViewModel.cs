using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;


namespace LSMEmprunts.EditableGridView
{
    /// <summary>
    /// Base class for the ViewModel of a ListView/GridView that uses EditableCell to allow to edit some cells of the grid view
    /// </summary>
    /// <typeparam name="TItem">Type of the items displayed in the GridView</typeparam>
    public partial class GridHostViewModel<TItem> : ReactiveObject, ICellEditHost
    {
        protected readonly ObservableCollection<TItem> _Items = [];

        /// <summary>
        /// The Items that will be displayed in the GridView. This shall be bound to the ListView.ItemsSource property.
        /// </summary>
        public ReadOnlyObservableCollection<TItem> Items {get;}

        /// <summary>
        /// Coordintaes (row/column) of the currently edited cell
        /// </summary>
        [Reactive]
        private CellId? _editingCell;

        /// <summary>
        /// if there is a currently edited cell, and if the item in the row implements IEditableObject, this field is set to the
        /// edited object, in order to BeginEdit/EndEdit/CancelEdit in the corresponding commands
        /// </summary>
        private IEditableObject _CurrentlyEditedObject;

        /// <summary>
        /// Command called to begin editing a given cell
        /// </summary>
        public ReactiveCommand<CellId, Unit> BeginEditCell { get; }
        
        /// <summary>
        /// Command called to end edition of the current cell, and commit the changes
        /// </summary>
        public ReactiveCommand<Unit, Unit> CommitEdit { get; }

        /// <summary>
        /// Command called to end edition of the current cell, and cancel the changes
        /// </summary>
        public ReactiveCommand<Unit, Unit> CancelEdit { get; }

        public GridHostViewModel()
        {
            Items = new ReadOnlyObservableCollection<TItem>(_Items);

            BeginEditCell = ReactiveCommand.Create<CellId>(cell =>
            {
                if (cell != EditingCell)
                {
                    if (cell.RowVm is IEditableObject editableObject && editableObject != _CurrentlyEditedObject)
                    {
                        _CurrentlyEditedObject?.EndEdit();
                        _CurrentlyEditedObject = editableObject;
                        editableObject?.BeginEdit();
                    }
                }

                EditingCell = cell;

            });
            CommitEdit = ReactiveCommand.Create(() =>
            {
                _CurrentlyEditedObject?.EndEdit();
                _CurrentlyEditedObject = null;
                EditingCell = null;
                OnCommitEdit();
            });
            CancelEdit = ReactiveCommand.Create(() =>
            {
                _CurrentlyEditedObject?.CancelEdit();
                _CurrentlyEditedObject = null;
                EditingCell = null;
                OnCancelEdit();
            });
        }

        protected virtual void OnCommitEdit()
        {
            //can be overridden in derived classes to add extra logic when an edit is committed (for example, saving changes to a database)
        }

        protected virtual void OnCancelEdit()
        {
            //can be overridden in derived classes to add extra logic when an edit is cancelled (for example, reverting changes in a database)
        }
    }
}
