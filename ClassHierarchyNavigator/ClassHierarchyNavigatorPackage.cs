using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace ClassHierarchyNavigator
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(ClassHierarchyNavigatorPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class ClassHierarchyNavigatorPackage : AsyncPackage
    {
        public const string PackageGuidString = "fa53d12f-084b-4624-98fa-915c486bb2c9";
        
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await NavigateToBaseCommand.InitializeAsync(this);
            await NavigateToDerivedCommand.InitializeAsync(this);
        }
    }
}
