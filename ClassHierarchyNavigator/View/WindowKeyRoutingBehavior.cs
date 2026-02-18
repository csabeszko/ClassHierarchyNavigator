using System.Windows;
using System.Windows.Input;

namespace ClassHierarchyNavigator.View
{
    public static class WindowKeyRoutingBehavior
    {
        public static readonly DependencyProperty PreviewKeyDownCommandProperty =
          DependencyProperty.RegisterAttached(
            "PreviewKeyDownCommand",
            typeof(ICommand),
            typeof(WindowKeyRoutingBehavior),
            new PropertyMetadata(null, HandlePreviewKeyDownCommandChanged));

        public static ICommand? GetPreviewKeyDownCommand(DependencyObject dependencyObject)
        {
            return (ICommand?)dependencyObject.GetValue(PreviewKeyDownCommandProperty);
        }

        public static void SetPreviewKeyDownCommand(DependencyObject dependencyObject, ICommand? value)
        {
            dependencyObject.SetValue(PreviewKeyDownCommandProperty, value);
        }

        private static void HandlePreviewKeyDownCommandChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArguments)
        {
            if (dependencyObject is not UIElement element)
            {
                return;
            }

            element.PreviewKeyDown -= HandlePreviewKeyDown;

            if (eventArguments.NewValue is ICommand)
            {
                element.PreviewKeyDown += HandlePreviewKeyDown;
            }
        }

        private static void HandlePreviewKeyDown(object sender, KeyEventArgs eventArguments)
        {
            var element = (UIElement)sender;
            var command = GetPreviewKeyDownCommand(element);

            if (command == null)
            {
                return;
            }

            if (command.CanExecute(eventArguments))
            {
                command.Execute(eventArguments);
                eventArguments.Handled = true;
            }
        }
    }
}
