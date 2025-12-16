using ClassHierarchyNavigator.Models;
using ClassHierarchyNavigator.Navigation;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FormsScreen = System.Windows.Forms.Screen;


namespace ClassHierarchyNavigator.UI
{
    public partial class TypeSelectionWindow : Window
    {
        public LeveledSymbol? SelectedSymbol { get; private set; }

        public string HeaderText { get; }

        public string HeaderArrow { get; }

        private readonly NavigationDirection navigationDirection;

        public TypeSelectionWindow(
            IReadOnlyList<LeveledSymbol> candidateSymbols,
            NavigationDirection direction)
        {
            InitializeComponent();

            DataContext = this;

            navigationDirection = direction;

            if (direction == NavigationDirection.Base)
            {
                HeaderText = "Find base types";
                HeaderArrow = "↑";
            }
            else
            {
                HeaderText = "Find derived types";
                HeaderArrow = "↑";
            }

            var items = candidateSymbols
                .Select(CreateItemFromSymbol)
                .ToList();

            TypeListBox.ItemsSource = items;
            TypeListBox.DisplayMemberPath = nameof(TypeSelectionItem.DisplayText);

            if (items.Count > 0)
            {
                TypeListBox.SelectedIndex = 0;
            }
        }

        private static TypeSelectionItem CreateItemFromSymbol(LeveledSymbol leveledSymbol)
        {
            return new TypeSelectionItem(leveledSymbol, $"{Indent(leveledSymbol.Level)}{leveledSymbol.Symbol.Name}");
        }

        private static string Indent(int level)
        {
            return new string(' ', level * 2);
        }

        private void HandleWindowKeyDown(object sender, KeyEventArgs keyEventArguments)
        {
            if (keyEventArguments.Key == Key.Enter)
            {
                CommitSelection();
                keyEventArguments.Handled = true;
                return;
            }

            if (keyEventArguments.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                keyEventArguments.Handled = true;
                return;
            }
        }

        private void HandleWindowLoaded(object sender, RoutedEventArgs eventArgs)
        {
            if (TypeListBox.Items.Count > 0 && TypeListBox.SelectedIndex < 0)
            {
                TypeListBox.SelectedIndex = 0;
            }

            TypeListBox.Focus();
            Keyboard.Focus(TypeListBox);

            AnimateDirectionArrowRotation();
            AdjustWindowWidthToLongestItem();
        }

        private void AdjustWindowWidthToLongestItem()
        {
            var items = TypeListBox.ItemsSource as IEnumerable<TypeSelectionItem>;
            if (items == null)
            {
                return;
            }

            var itemList = items.ToList();
            if (itemList.Count == 0)
            {
                return;
            }

            var typeface = new Typeface(TypeListBox.FontFamily, TypeListBox.FontStyle, TypeListBox.FontWeight, TypeListBox.FontStretch);
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            double maximumTextWidth = 0.0;

            foreach (var item in itemList)
            {
                if (string.IsNullOrWhiteSpace(item.DisplayText))
                {
                    continue;
                }

                var formattedText = new FormattedText(
                    item.DisplayText,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    TypeListBox.FontSize,
                    Brushes.Black,
                    pixelsPerDip);

                double currentWidth = formattedText.WidthIncludingTrailingWhitespace;
                if (currentWidth > maximumTextWidth)
                {
                    maximumTextWidth = currentWidth;
                }
            }

            Point windowScreenPoint = PointToScreen(new Point(0, 0));
            var screen = FormsScreen.FromPoint(new System.Drawing.Point((int)windowScreenPoint.X, (int)windowScreenPoint.Y));
            var workingArea = screen.WorkingArea;


            double outerPaddingWidth = 80.0;
            double desiredWidth = maximumTextWidth + outerPaddingWidth;

            double maximumAllowedWidth = workingArea.Width - 20.0;
            if (desiredWidth > maximumAllowedWidth)
            {
                desiredWidth = maximumAllowedWidth;
            }

            if (desiredWidth < Width)
            {
                desiredWidth = Width;
            }

            Width = desiredWidth;
        }

        private void AnimateDirectionArrowRotation()
        {
            double targetAngle;

            if (navigationDirection == NavigationDirection.Base)
            {
                targetAngle = 0.0;
            }
            else
            {
                targetAngle = 180.0;
            }

            var animation = new DoubleAnimation
            {
                To = targetAngle,
                Duration = TimeSpan.FromMilliseconds(140),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            DirectionArrowRotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
        }

        private void HandleListBoxMouseDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArguments)
        {
            CommitSelection();
        }

        private void CommitSelection()
        {
            var selectedItem = TypeListBox.SelectedItem as TypeSelectionItem;
            if (selectedItem == null)
            {
                return;
            }

            SelectedSymbol = selectedItem.Symbol;
            DialogResult = true;
            Close();
        }

        private sealed class TypeSelectionItem
        {
            public LeveledSymbol Symbol { get; }

            public string DisplayText { get; }

            public TypeSelectionItem(LeveledSymbol symbol, string displayText)
            {
                Symbol = symbol;
                DisplayText = displayText;
            }
        }
    }
}
