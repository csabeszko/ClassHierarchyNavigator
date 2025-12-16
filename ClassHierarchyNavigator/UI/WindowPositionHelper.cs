using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System;
using System.Windows;
using System.Windows.Forms;

namespace ClassHierarchyNavigator.UI
{
    public static class WindowPositionHelper
    {
        public static void PositionAroundSpan(Window window, IWpfTextView wpfTextView, SnapshotSpan snapshotSpan, bool preferAbove)
        {
            if (window == null)
            {
                return;
            }

            if (wpfTextView == null)
            {
                return;
            }

            if (snapshotSpan.Snapshot != wpfTextView.TextSnapshot)
            {
                snapshotSpan = snapshotSpan.TranslateTo(wpfTextView.TextSnapshot, SpanTrackingMode.EdgeInclusive);
            }

            wpfTextView.ViewScroller.EnsureSpanVisible(snapshotSpan, EnsureSpanVisibleOptions.MinimumScroll);
            wpfTextView.VisualElement.UpdateLayout();

            SnapshotPoint startPoint = snapshotSpan.Start;
            SnapshotPoint endPoint = snapshotSpan.End;

            ITextViewLine startLine = wpfTextView.TextViewLines.GetTextViewLineContainingBufferPosition(startPoint);
            if (startLine == null)
            {
                return;
            }

            ITextViewLine endLine = wpfTextView.TextViewLines.GetTextViewLineContainingBufferPosition(endPoint) ?? startLine;

            TextBounds startBounds = startLine.GetCharacterBounds(startPoint);

            TextBounds endBounds;
            if (snapshotSpan.Length == 0)
            {
                endBounds = startBounds;
            }
            else
            {
                SnapshotPoint endForBounds = endPoint.Position > 0
                    ? new SnapshotPoint(endPoint.Snapshot, endPoint.Position - 1)
                    : endPoint;

                endBounds = endLine.GetCharacterBounds(endForBounds);
            }

            double spanCenterX = (startBounds.Left + endBounds.Right) / 2.0;

            Point belowViewPoint = new Point(spanCenterX, startBounds.Bottom);
            Point aboveViewPoint = new Point(spanCenterX, startBounds.Top);

            Point belowScreenPoint = wpfTextView.VisualElement.PointToScreen(belowViewPoint);
            Point aboveScreenPoint = wpfTextView.VisualElement.PointToScreen(aboveViewPoint);

            window.WindowStartupLocation = WindowStartupLocation.Manual;

            window.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double windowWidth = double.IsNaN(window.Width) ? window.DesiredSize.Width : window.Width;
            double windowHeight = double.IsNaN(window.Height) ? window.DesiredSize.Height : window.Height;

            double horizontalLeft = belowScreenPoint.X - (windowWidth / 2.0);

            Screen screen = Screen.FromPoint(new System.Drawing.Point((int)belowScreenPoint.X, (int)belowScreenPoint.Y));
            System.Drawing.Rectangle workingArea = screen.WorkingArea;

            double verticalGap = 6.0;

            double topIfBelow = belowScreenPoint.Y + verticalGap;
            double topIfAbove = aboveScreenPoint.Y - windowHeight - verticalGap;

            bool canPlaceBelow = topIfBelow + windowHeight <= workingArea.Bottom;
            bool canPlaceAbove = topIfAbove >= workingArea.Top;

            double chosenTop;

            if (preferAbove)
            {
                chosenTop = canPlaceAbove ? topIfAbove : topIfBelow;
            }
            else
            {
                chosenTop = canPlaceBelow ? topIfBelow : topIfAbove;
            }

            window.Left = horizontalLeft;
            window.Top = chosenTop;

            ClampWindowToMonitor(window, screen, windowWidth, windowHeight);
        }

        private static void ClampWindowToMonitor(Window window, Screen screen, double windowWidth, double windowHeight)
        {
            System.Drawing.Rectangle workingArea = screen.WorkingArea;

            double leftBound = workingArea.Left;
            double topBound = workingArea.Top;
            double rightBound = workingArea.Right;
            double bottomBound = workingArea.Bottom;

            if (window.Left + windowWidth > rightBound)
            {
                window.Left = rightBound - windowWidth;
            }

            if (window.Left < leftBound)
            {
                window.Left = leftBound;
            }

            if (window.Top + windowHeight > bottomBound)
            {
                window.Top = bottomBound - windowHeight;
            }

            if (window.Top < topBound)
            {
                window.Top = topBound;
            }
        }
    }
}
