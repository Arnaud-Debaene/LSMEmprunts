using ReactiveUI;
using System.Reactive;

namespace LSMEmprunts
{
    /// <summary>
    /// Description of a GridView cell/coordinates
    /// </summary>
    /// <param name="RowVm">The ViewModel of one of the GridView rows (that is, on e of the objects displayed in the GridView)</param>
    /// <param name="ColumnKey">Key identifiying a clumn in the GridView</param>
    public readonly record struct CellId(object RowVm, string ColumnKey);

    /// <summary>
    /// interface that must be implemented DataContext/ViewModel of a GridView hosting EditableCells, 
    /// to allow the latter to coordinate edit operations (begin, commit, cancel) and know which cell is being edited
    /// </summary>
    public interface ICellEditHost
    {
        CellId? EditingCell { get; set; }

        ReactiveCommand<CellId, Unit> BeginEditCell { get; }
        ReactiveCommand<Unit, Unit> CommitEdit { get; }
        ReactiveCommand<Unit, Unit> CancelEdit { get; }
    }
}
