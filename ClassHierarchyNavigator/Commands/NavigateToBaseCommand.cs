using ClassHierarchyNavigator.Commands;
using ClassHierarchyNavigator.Navigation;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace ClassHierarchyNavigator
{
    public sealed class NavigateToBaseCommand : AbstractNavigateCommand
    {
        public const int CommandId = 0x0100;

        public static NavigateToBaseCommand Instance { get; private set; } = null!;

        private NavigateToBaseCommand(
            AsyncPackage package,
            OleMenuCommandService commandService)
            : base(CommandId, NavigationDirection.Base, package, commandService)
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

            Instance = new NavigateToBaseCommand(package, commandService);
        }
    }
}
