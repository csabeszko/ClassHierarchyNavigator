using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Windows;
using System.Windows.Interop;

namespace ClassHierarchyNavigator.View
{
    public static class WindowPositionHelper
    {
        public static void CenterOnVisualStudio(Window window)
        {
            if (window == null)
            {
                return;
            }

            window.Topmost = false;

            var visualStudioOwnerHandle = GetVisualStudioDialogOwnerHandle();
            if (visualStudioOwnerHandle == IntPtr.Zero)
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            var windowInteropHelper = new WindowInteropHelper(window);
            windowInteropHelper.Owner = visualStudioOwnerHandle;

            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        private static IntPtr GetVisualStudioDialogOwnerHandle()
        {
            var uiShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
            if (uiShell == null)
            {
                return IntPtr.Zero;
            }

            uiShell.GetDialogOwnerHwnd(out var ownerHandle);
            return ownerHandle;
        }
    }
}
