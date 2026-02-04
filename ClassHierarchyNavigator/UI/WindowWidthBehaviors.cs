using ClassHierarchyNavigator.ViewModels;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClassHierarchyNavigator.UI
{
    public static class WindowWidthBehaviors
    {
        public static readonly DependencyProperty IsEnabledProperty =
          DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(WindowWidthBehaviors),
            new PropertyMetadata(false, HandleIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject dependencyObject)
        {
            return (bool)dependencyObject.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject dependencyObject, bool value)
        {
            dependencyObject.SetValue(IsEnabledProperty, value);
        }

        private static void HandleIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArguments)
        {
            if (dependencyObject is not ListBox listBox)
            {
                return;
            }

            if ((bool)eventArguments.NewValue)
            {
                listBox.Loaded += HandleLoaded;
            }
            else
            {
                listBox.Loaded -= HandleLoaded;
            }
        }

        private static void HandleLoaded(object sender, RoutedEventArgs eventArguments)
        {
            var listBox = (ListBox)sender;

            listBox.Loaded -= HandleLoaded;

            if (listBox.DataContext is not TypeSelectionViewModel viewModel)
            {
                return;
            }

            void UpdateWidth()
            {
                var width = CalculateDesiredWidth(listBox, listBox.ItemsSource);
                if (width > 0)
                {
                    viewModel.WindowWidth = width;
                }
            }

            UpdateWidth();

            var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
            descriptor.AddValueChanged(listBox, (_, __) => UpdateWidth());
        }

        private static double CalculateDesiredWidth(ListBox listBox, IEnumerable? itemsSource)
        {
            if (itemsSource == null)
            {
                return 0.0;
            }

            var items = itemsSource.Cast<object>().ToList();
            if (items.Count == 0)
            {
                return 0.0;
            }

            var window = Window.GetWindow(listBox);
            if (window == null)
            {
                return 0.0;
            }

            var typeface = new Typeface(listBox.FontFamily, listBox.FontStyle, listBox.FontWeight, listBox.FontStretch);
            var pixelsPerDip = VisualTreeHelper.GetDpi(window).PixelsPerDip;

            double maximumTextWidth = 0.0;

            foreach (var item in items)
            {
                string text = item switch
                {
                    GroupHeaderEntry header => header.Title,
                    SymbolEntry symbol => symbol.DisplayName,
                    _ => string.Empty
                };

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var formattedText = new FormattedText(
                  text,
                  CultureInfo.CurrentUICulture,
                  FlowDirection.LeftToRight,
                  typeface,
                  listBox.FontSize,
                  Brushes.Black,
                  pixelsPerDip);

                var currentWidth = formattedText.WidthIncludingTrailingWhitespace;
                if (currentWidth > maximumTextWidth)
                {
                    maximumTextWidth = currentWidth;
                }
            }

            var screenPoint = System.Windows.Forms.Cursor.Position;
            var screen = System.Windows.Forms.Screen.FromPoint(screenPoint);
            var workingArea = screen.WorkingArea;

            var outerPaddingWidth = 80.0;
            var desiredWidth = maximumTextWidth + outerPaddingWidth;

            var maximumAllowedWidth = workingArea.Width - 20.0;
            if (desiredWidth > maximumAllowedWidth)
            {
                desiredWidth = maximumAllowedWidth;
            }

            if (desiredWidth < 420.0)
            {
                desiredWidth = 420.0;
            }

            return desiredWidth;
        }
    }
}
