using System;
using System.Windows;
using System.Windows.Interop;

namespace ClassHierarchyNavigator.View
{
    public static class WindowPositionHelper
    {
        public static void CenterOnCurrentMonitor(Window window)
        {
            if (window == null)
            {
                return;
            }

            window.WindowStartupLocation = WindowStartupLocation.Manual;

            if (window.IsLoaded == false)
            {
                window.Loaded += HandleWindowLoaded;
                return;
            }

            CenterNow(window);
        }

        private static void HandleWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Window window)
            {
                return;
            }

            window.Loaded -= HandleWindowLoaded;

            CenterNow(window);
        }

        private static void CenterNow(Window window)
        {
            var windowHandle = new WindowInteropHelper(window).Handle;

            var screen = windowHandle != IntPtr.Zero
                ? System.Windows.Forms.Screen.FromHandle(windowHandle)
                : System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);

            var workingAreaPixels = screen.WorkingArea;

            var dpiScale = GetDpiScale(window);

            var workingAreaLeftDip = workingAreaPixels.Left / dpiScale.DpiScaleX;
            var workingAreaTopDip = workingAreaPixels.Top / dpiScale.DpiScaleY;
            var workingAreaWidthDip = workingAreaPixels.Width / dpiScale.DpiScaleX;
            var workingAreaHeightDip = workingAreaPixels.Height / dpiScale.DpiScaleY;

            var windowWidthDip = window.ActualWidth;
            var windowHeightDip = window.ActualHeight;

            if (windowWidthDip <= 0.0)
            {
                windowWidthDip = window.Width;
            }

            if (windowHeightDip <= 0.0)
            {
                windowHeightDip = window.Height;
            }

            if (double.IsNaN(windowWidthDip) || windowWidthDip <= 0.0)
            {
                windowWidthDip = 420.0;
            }

            if (double.IsNaN(windowHeightDip) || windowHeightDip <= 0.0)
            {
                windowHeightDip = 260.0;
            }

            window.Left = workingAreaLeftDip + (workingAreaWidthDip - windowWidthDip) / 2.0;
            window.Top = workingAreaTopDip + (workingAreaHeightDip - windowHeightDip) / 2.0;
        }

        private static DpiScale GetDpiScale(Window window)
        {
            var presentationSource = PresentationSource.FromVisual(window);
            if (presentationSource?.CompositionTarget == null)
            {
                return new DpiScale(1.0, 1.0);
            }

            return new DpiScale(
                presentationSource.CompositionTarget.TransformToDevice.M11,
                presentationSource.CompositionTarget.TransformToDevice.M22);
        }
    }
}
