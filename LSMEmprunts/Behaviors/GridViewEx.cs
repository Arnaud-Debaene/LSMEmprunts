using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LSMEmprunts
{
    /// <summary>
    /// Attached properties for GridView (the one used in a WPF ListView)
    /// </summary>
    public static class GridViewEx
    {
        #region HorizontalContentAlignment attached property
        /// <summary>
        /// Gets or sets the horizontal content alignment for GridView cells.
        /// This attached property controls how content is aligned horizontally within grid view columns.
        /// </summary>
        public static readonly DependencyProperty HorizontalContentAlignmentProperty =
            DependencyProperty.RegisterAttached("HorizontalContentAlignment", typeof(HorizontalAlignment),
                typeof(GridViewEx), new PropertyMetadata(HorizontalAlignment.Left, OnHorizontalContentAlignmentChanged));

        /// <summary>
        /// Gets the horizontal content alignment value for the specified dependency object.
        /// </summary>
        /// <param name="obj">The dependency object to get the alignment from.</param>
        /// <returns>The horizontal alignment value.</returns>
        public static HorizontalAlignment GetHorizontalContentAlignment(DependencyObject obj) => (HorizontalAlignment)obj.GetValue(HorizontalContentAlignmentProperty);

        /// <summary>
        /// Sets the horizontal content alignment value for the specified dependency object.
        /// </summary>
        /// <param name="obj">The dependency object to set the alignment on.</param>
        /// <param name="value">The horizontal alignment value to apply.</param>
        public static void SetHorizontalContentAlignment(DependencyObject obj, HorizontalAlignment value) => obj.SetValue(HorizontalContentAlignmentProperty, value);

        private static void OnHorizontalContentAlignmentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var current = sender;
            int levels = 10;
            /*walk up the visual tree to find the ContentPresenter that is a child of the GridViewRowPresenter (that is the layout of a GridView row), 
             * and set its HorizontalAlignment*/
            while (current != null && levels-- > 0)
            {
                var next = VisualTreeHelper.GetParent(current);
                if (next is GridViewRowPresenter)
                {
                    var cp = current as ContentPresenter;
                    if (cp != null)
                    {
                        cp.HorizontalAlignment = (HorizontalAlignment)args.NewValue;
                    }
                    break;
                }

                current = next;
            }
        }
        #endregion

        #region VerticalContentAlignment attached property
        /// <summary>
        /// Gets or sets the vertical content alignment for GridView cells.
        /// This attached property controls how content is aligned vertically within grid view columns.
        /// </summary>
        public static readonly DependencyProperty VerticalContentAlignmentProperty =
            DependencyProperty.RegisterAttached("VerticalContentAlignment", typeof(VerticalAlignment),
                typeof(GridViewEx), new PropertyMetadata(VerticalAlignment.Center, OnVerticalContentAlignmentChanged));

        /// <summary>
        /// Gets the vertical content alignment value for the specified dependency object.
        /// </summary>
        /// <param name="obj">The dependency object to get the alignment from.</param>
        /// <returns>The vertical alignment value.</returns>
        public static VerticalAlignment GetVerticalContentAlignment(DependencyObject obj) => (VerticalAlignment)obj.GetValue(VerticalContentAlignmentProperty);

        /// <summary>
        /// Sets the vertical content alignment value for the specified dependency object.
        /// </summary>
        /// <param name="obj">The dependency object to set the alignment on.</param>
        /// <param name="value">The vertical alignment value to apply.</param>
        public static void SetVerticalContentAlignment(DependencyObject obj, VerticalAlignment value) => obj.SetValue(VerticalContentAlignmentProperty, value);

        private static void OnVerticalContentAlignmentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var current = sender;
            int levels = 10;
            /*walk up the visual tree to find the ContentPresenter that is a child of the GridViewRowPresenter (that is the layout of a GridView row), 
             * and set its VerticalAlignment*/
            while (current != null && levels-- > 0)
            {
                var next = VisualTreeHelper.GetParent(current);
                if (next is GridViewRowPresenter)
                {
                    var cp = current as ContentPresenter;
                    if (cp != null)
                    {
                        cp.VerticalAlignment = (VerticalAlignment)args.NewValue;
                    }
                    break;
                }

                current = next;
            }
        }
        #endregion
    }
}
