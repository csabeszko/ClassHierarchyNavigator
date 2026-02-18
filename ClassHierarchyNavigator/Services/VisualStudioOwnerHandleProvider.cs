using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;

namespace ClassHierarchyNavigator.Services
{
    internal static class VisualStudioOwnerHandleProvider
    {
        public static async Task<IntPtr> GetOwnerHandleAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var uiShell = await package.GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;
            if (uiShell == null)
            {
                return IntPtr.Zero;
            }

            uiShell.GetDialogOwnerHwnd(out var ownerHandle);
            return ownerHandle;
        }
    }
}
