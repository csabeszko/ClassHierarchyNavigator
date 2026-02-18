using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ClassHierarchyNavigator.View
{
    public static class ListBoxScrollIntoViewBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ListBoxScrollIntoViewBehavior),
                new PropertyMetadata(false, HandleIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject dependencyObject)
        {
            return (bool)dependencyObject.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject dependencyObject, bool value)
        {
            dependencyObject.SetValue(IsEnabledProperty, value);
        }

        private static void HandleIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            if (dependencyObject is not ListBox listBox)
            {
                return;
            }

            if (eventArgs.NewValue is bool isEnabled && isEnabled)
            {
                listBox.SelectionChanged += HandleSelectionChanged;
            }
            else
            {
                listBox.SelectionChanged -= HandleSelectionChanged;
            }
        }

        private static void HandleSelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
        {
            if (sender is not ListBox listBox)
            {
                return;
            }

            var selectedItem = listBox.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            _ = listBox.Dispatcher.InvokeAsync(
                () =>
                {
                    listBox.UpdateLayout();
                    listBox.ScrollIntoView(selectedItem);
                },
                DispatcherPriority.Loaded);
        }
    }
}
