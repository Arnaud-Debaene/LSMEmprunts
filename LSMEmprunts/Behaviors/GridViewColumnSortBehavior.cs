using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace LSMEmprunts
{
    /// <summary>
    /// A behavior to add a sorting mechanism to GridView columns.
    /// Attach this behavior to a `ListView` and specify the sort property for each column using the attached `Sort` property.
    /// </summary>
    public sealed class GridViewColumnSortBehavior : Behavior<ListView>
    {
        /// <summary>
        /// Attached property to set on a column indicating the sort property associated with that column.
        /// If this property is not set, the behavior will try to use the sort property from the column's `DisplayMemberBinding`.
        /// </summary>
        private static DependencyProperty SortProperty = DependencyProperty.RegisterAttached("Sort", typeof(string), typeof(GridViewColumnSortBehavior),
            new PropertyMetadata(null));

        public static void SetSort(DependencyObject element, string value) => element.SetValue(SortProperty, value);

        public static string GetSort(DependencyObject element) => (string)element.GetValue(SortProperty);

        /// <summary>
        /// Attached property to set on a column to indicate whether sorting is allowed on that column. Sorting is allowed by default.
        /// </summary>
        private static DependencyProperty AllowSortProperty = DependencyProperty.RegisterAttached("AllowSort", typeof(bool), typeof(GridViewColumnSortBehavior),
            new PropertyMetadata(true));

        public static void SetAllowSort(DependencyObject element, bool value) => element.SetValue(AllowSortProperty, value);

        public static bool GetAllowSort(DependencyObject element) => (bool)element.GetValue(AllowSortProperty);

        protected override void OnAttached()
        {
            AssociatedObject.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(OnColumnHeaderClick));
        }

        protected override void OnDetaching()
        {
            AssociatedObject.RemoveHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(OnColumnHeaderClick));
        }

        private void OnColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            var columnHeader = e.OriginalSource as GridViewColumnHeader;

            if (columnHeader!=null && columnHeader.Role!=GridViewColumnHeaderRole.Padding)
            {
                var sortAllowed = GetAllowSort(columnHeader);
                if (!sortAllowed)
                {
                    return;
                }

                var column = columnHeader.Column;
                var sortProperty = GetSort(column);
                if (string.IsNullOrEmpty(sortProperty)) 
                {
                    //try to find the sort property in the column's display member binding                    
                    if (column.DisplayMemberBinding is System.Windows.Data.Binding binding)
                    {
                        sortProperty = binding.Path.Path;
                    }
                }

                if (!string.IsNullOrEmpty(sortProperty))
                {
                    var sourceCollection = AssociatedObject.Items as ICollectionView;
                    if (sourceCollection != null)
                    {
                        var currentSort = sourceCollection.SortDescriptions.FirstOrDefault();
                        var newDirection = ListSortDirection.Ascending;
                        if (currentSort.PropertyName == sortProperty && currentSort.Direction == ListSortDirection.Ascending)
                        {
                            newDirection = ListSortDirection.Descending;
                        }
                        sourceCollection.SortDescriptions.Clear();
                        sourceCollection.SortDescriptions.Add(new SortDescription(sortProperty, newDirection));
                        SetSortGlyph(columnHeader, newDirection);
                    }
                }
            }
        }

        /// <summary>
        /// Stores the last header where a sort adorner was added, so it can be removed before adding a new one.
        /// </summary>
        private GridViewColumnHeader _HeaderWithSortAdorner = null;

        private void SetSortGlyph(GridViewColumnHeader columnHeader, ListSortDirection direction)
        {
            RemoveSortGlyph();
            var adornerLayer = AdornerLayer.GetAdornerLayer(columnHeader);
            adornerLayer.Add(new SortGlyphAdorner(columnHeader, direction));
            _HeaderWithSortAdorner = columnHeader;
        }

        private void RemoveSortGlyph()
        {
            if (_HeaderWithSortAdorner!=null)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(_HeaderWithSortAdorner);
                var adorners = adornerLayer.GetAdorners(_HeaderWithSortAdorner);
                foreach(var adorner in (adorners ?? Enumerable.Empty<Adorner>()).OfType<SortGlyphAdorner>())
                {
                    adornerLayer.Remove(adorner);
                }
                _HeaderWithSortAdorner = null;
            }
        }


    }

    /// <summary>
    /// Adorner to draw a sort arrow in a `GridViewColumnHeader`.
    /// </summary>
    public sealed class SortGlyphAdorner : Adorner
    {
        private static Geometry ascGeometry =
       Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

        private static Geometry descGeometry =
            Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

        public ListSortDirection Direction { get; }

        public SortGlyphAdorner(UIElement element, ListSortDirection dir)
        : base(element)
        {
            Direction = dir;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (AdornedElement.RenderSize.Width < 20)
                return;

            TranslateTransform transform = new TranslateTransform
                (
                    AdornedElement.RenderSize.Width - 15,
                    (AdornedElement.RenderSize.Height - 5) / 2
                );
            drawingContext.PushTransform(transform);

            var geometry = ascGeometry;
            if (this.Direction == ListSortDirection.Descending)
                geometry = descGeometry;
            drawingContext.DrawGeometry(Brushes.Black, null, geometry);

            drawingContext.Pop();
        }
    }

}
