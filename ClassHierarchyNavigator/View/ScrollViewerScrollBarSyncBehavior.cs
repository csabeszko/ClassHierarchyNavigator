using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ClassHierarchyNavigator.View
{
    public static class ScrollViewerScrollBarSyncBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ScrollViewerScrollBarSyncBehavior),
                new PropertyMetadata(false, HandleIsEnabledChanged));

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached(
                "IsUpdating",
                typeof(bool),
                typeof(ScrollViewerScrollBarSyncBehavior),
                new PropertyMetadata(false));

        private static readonly DependencyProperty ScrollBarProperty =
            DependencyProperty.RegisterAttached(
                "ScrollBar",
                typeof(ScrollBar),
                typeof(ScrollViewerScrollBarSyncBehavior),
                new PropertyMetadata(null));

        public static void SetIsEnabled(DependencyObject element, bool value)
        {
            element.SetValue(IsEnabledProperty, value);
        }

        public static bool GetIsEnabled(DependencyObject element)
        {
            return (bool)element.GetValue(IsEnabledProperty);
        }

        private static void HandleIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            if (dependencyObject is not ScrollViewer scrollViewer)
            {
                return;
            }

            var isEnabled = (bool)eventArgs.NewValue;

            if (isEnabled)
            {
                scrollViewer.Loaded -= HandleScrollViewerLoaded;
                scrollViewer.Loaded += HandleScrollViewerLoaded;

                scrollViewer.Unloaded -= HandleScrollViewerUnloaded;
                scrollViewer.Unloaded += HandleScrollViewerUnloaded;

                scrollViewer.ScrollChanged -= HandleScrollViewerScrollChanged;
                scrollViewer.ScrollChanged += HandleScrollViewerScrollChanged;
            }
            else
            {
                Detach(scrollViewer);
            }
        }

        private static void HandleScrollViewerLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
            {
                return;
            }

            Attach(scrollViewer);
            SyncFromScrollViewer(scrollViewer);
        }

        private static void HandleScrollViewerUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
            {
                return;
            }

            Detach(scrollViewer);
        }

        private static void Attach(ScrollViewer scrollViewer)
        {
            if (scrollViewer.Template == null)
            {
                return;
            }

            if (scrollViewer.Template.FindName("PART_VerticalScrollBar", scrollViewer) is not ScrollBar scrollBar)
            {
                return;
            }

            var existingScrollBar = (ScrollBar)scrollViewer.GetValue(ScrollBarProperty);
            if (ReferenceEquals(existingScrollBar, scrollBar))
            {
                return;
            }

            if (existingScrollBar != null)
            {
                existingScrollBar.ValueChanged -= HandleScrollBarValueChanged;
            }

            scrollViewer.SetValue(ScrollBarProperty, scrollBar);

            scrollBar.ValueChanged -= HandleScrollBarValueChanged;
            scrollBar.ValueChanged += HandleScrollBarValueChanged;
        }

        private static void Detach(ScrollViewer scrollViewer)
        {
            scrollViewer.Loaded -= HandleScrollViewerLoaded;
            scrollViewer.Unloaded -= HandleScrollViewerUnloaded;
            scrollViewer.ScrollChanged -= HandleScrollViewerScrollChanged;

            var scrollBar = (ScrollBar)scrollViewer.GetValue(ScrollBarProperty);
            if (scrollBar != null)
            {
                scrollBar.ValueChanged -= HandleScrollBarValueChanged;
            }

            scrollViewer.ClearValue(ScrollBarProperty);
        }

        private static void HandleScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
            {
                return;
            }

            Attach(scrollViewer);
            SyncFromScrollViewer(scrollViewer);
        }

        private static void SyncFromScrollViewer(ScrollViewer scrollViewer)
        {
            var scrollBar = (ScrollBar)scrollViewer.GetValue(ScrollBarProperty);
            if (scrollBar == null)
            {
                return;
            }

            var isUpdating = (bool)scrollViewer.GetValue(IsUpdatingProperty);
            if (isUpdating)
            {
                return;
            }

            scrollViewer.SetValue(IsUpdatingProperty, true);
            try
            {
                scrollBar.Minimum = 0;
                scrollBar.Maximum = scrollViewer.ScrollableHeight;
                scrollBar.ViewportSize = scrollViewer.ViewportHeight;
                scrollBar.LargeChange = scrollViewer.ViewportHeight;
                scrollBar.Value = scrollViewer.VerticalOffset;
            }
            finally
            {
                scrollViewer.SetValue(IsUpdatingProperty, false);
            }
        }

        private static void HandleScrollBarValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is not ScrollBar scrollBar)
            {
                return;
            }

            var scrollViewer = FindScrollViewerForScrollBar(scrollBar);
            if (scrollViewer == null)
            {
                return;
            }

            var isUpdating = (bool)scrollViewer.GetValue(IsUpdatingProperty);
            if (isUpdating)
            {
                return;
            }

            scrollViewer.SetValue(IsUpdatingProperty, true);
            try
            {
                scrollViewer.ScrollToVerticalOffset(e.NewValue);
            }
            finally
            {
                scrollViewer.SetValue(IsUpdatingProperty, false);
            }
        }

        private static ScrollViewer FindScrollViewerForScrollBar(ScrollBar scrollBar)
        {
            var current = (DependencyObject)scrollBar;
            while (current != null)
            {
                if (current is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }

                current = LogicalTreeHelper.GetParent(current) ?? System.Windows.Media.VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
