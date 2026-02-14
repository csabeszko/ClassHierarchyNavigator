using ClassHierarchyNavigator.Commands;
using ClassHierarchyNavigator.Navigation;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace ClassHierarchyNavigator
{
    public sealed class NavigateToDerivedCommand : AbstractNavigateCommand
    {
        public const int CommandId = 0x0101;

        public static NavigateToDerivedCommand Instance { get; private set; } = null!;

        private NavigateToDerivedCommand(
            AsyncPackage package,
            OleMenuCommandService commandService)
            : base(CommandId, NavigationDirection.Derived, package, commandService)
        {
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var serviceObject = await package.GetServiceAsync(typeof(IMenuCommandService));
            if (serviceObject is not OleMenuCommandService commandService)
            {
                throw new InvalidOperationException("IMenuCommandService is not available.");
            }

            Instance = new NavigateToDerivedCommand(package, commandService);
        }
    }
}
