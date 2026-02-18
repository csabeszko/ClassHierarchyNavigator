using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClassHierarchyNavigator.View
{
    public static class ListBoxInputBehavior
    {
        public static readonly DependencyProperty DoubleClickCommandProperty =
          DependencyProperty.RegisterAttached(
            "DoubleClickCommand",
            typeof(ICommand),
            typeof(ListBoxInputBehavior),
            new PropertyMetadata(null, HandleDoubleClickCommandChanged));

        public static ICommand? GetDoubleClickCommand(DependencyObject dependencyObject)
        {
            return (ICommand?)dependencyObject.GetValue(DoubleClickCommandProperty);
        }

        public static void SetDoubleClickCommand(DependencyObject dependencyObject, ICommand? value)
        {
            dependencyObject.SetValue(DoubleClickCommandProperty, value);
        }

        private static void HandleDoubleClickCommandChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArguments)
        {
            if (dependencyObject is not Control control)
            {
                return;
            }

            control.MouseDoubleClick -= HandleMouseDoubleClick;
            if (eventArguments.NewValue is ICommand)
            {
                control.MouseDoubleClick += HandleMouseDoubleClick;
            }
        }

        private static void HandleMouseDoubleClick(object sender, MouseButtonEventArgs eventArguments)
        {
            var control = (Control)sender;
            var command = GetDoubleClickCommand(control);

            if (command == null)
            {
                return;
            }

            if (command.CanExecute(null))
            {
                command.Execute(null);
            }
        }
    }
}
