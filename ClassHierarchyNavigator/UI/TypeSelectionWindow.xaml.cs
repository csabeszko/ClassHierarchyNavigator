using ClassHierarchyNavigator.Models;
using ClassHierarchyNavigator.Navigation;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ClassHierarchyNavigator.UI
{
    public partial class TypeSelectionWindow : Window, INotifyPropertyChanged
    {
        public LeveledSymbol? SelectedSymbol { get; private set; }

        public string HeaderText { get; }

        public string HeaderArrow { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string SearchText
        {
            get
            {
                return searchText;
            }
            set
            {
                var normalizedValue = value ?? string.Empty;
                if (string.Equals(searchText, normalizedValue, StringComparison.Ordinal))
                {
                    return;
                }

                searchText = normalizedValue;
                OnPropertyChanged();
                RebuildEntries();
            }
        }

        public string StatusText
        {
            get
            {
                return statusText;
            }
            private set
            {
                if (string.Equals(statusText, value ?? string.Empty, StringComparison.Ordinal))
                {
                    return;
                }

                statusText = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasStatus));
            }
        }

        public bool HasStatus
        {
            get
            {
                return !string.IsNullOrWhiteSpace(statusText);
            }
        }

        public string WarningText
        {
            get
            {
                return warningText;
            }
            private set
            {
                if (string.Equals(warningText, value ?? string.Empty, StringComparison.Ordinal))
                {
                    return;
                }

                warningText = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasWarning));
            }
        }

        public bool HasWarning
        {
            get
            {
                return !string.IsNullOrWhiteSpace(warningText);
            }
        }

        private readonly NavigationDirection navigationDirection;
        private readonly IReadOnlyList<LeveledSymbol> allCandidateSymbols;
        private readonly INamedTypeSymbol targetTypeSymbol;
        private readonly bool isTargetInterface;

        private string searchText = string.Empty;
        private string statusText = string.Empty;
        private string warningText = string.Empty;

        public TypeSelectionWindow(
            IReadOnlyList<LeveledSymbol> candidateSymbols,
            NavigationDirection direction,
            INamedTypeSymbol targetTypeSymbol,
            string? warningText = null,
            string? statusText = null)
        {
            InitializeComponent();

            DataContext = this;

            navigationDirection = direction;

            allCandidateSymbols = candidateSymbols ?? Array.Empty<LeveledSymbol>();
            this.targetTypeSymbol = targetTypeSymbol;
            isTargetInterface = targetTypeSymbol.TypeKind == TypeKind.Interface;

            var targetTypeDisplayName = TypeDisplayNameProvider.GetDisplayName(targetTypeSymbol);

            if (direction == NavigationDirection.Base)
            {
                HeaderText = $"Find base types of {targetTypeDisplayName}";
                HeaderArrow = "↑";
            }
            else
            {
                HeaderText = $"Find derived types of {targetTypeDisplayName}";
                HeaderArrow = "↓";
            }

            WarningText = warningText ?? string.Empty;
            StatusText = statusText ?? string.Empty;

            RebuildEntries();
        }

        private void RebuildEntries()
        {
            var normalizedSearch = (searchText ?? string.Empty).Trim();

            IEnumerable<LeveledSymbol> filtered = allCandidateSymbols;

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                filtered = filtered.Where(symbol => MatchesFilter(symbol, normalizedSearch));
            }

            var filteredList = filtered.ToList();

            var entries = BuildEntries(filteredList);

            TypeListBox.ItemsSource = entries;

            if (entries.OfType<SymbolEntry>().Any())
            {
                TypeListBox.SelectedItem = entries.OfType<SymbolEntry>().FirstOrDefault();
                TypeListBox.IsEnabled = true;
                StatusText = string.Empty;
            }
            else
            {
                TypeListBox.IsEnabled = false;

                if (allCandidateSymbols.Count == 0)
                {
                    StatusText = "No results.";
                }
                else
                {
                    StatusText = "No results (after filtering).";
                }
            }

            AdjustWindowWidthToLongestItem(entries);
        }

        private static bool MatchesFilter(LeveledSymbol leveledSymbol, string filter)
        {
            var displayName = TypeDisplayNameProvider.GetDisplayName(leveledSymbol.Symbol);

            if (displayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var name = leveledSymbol.Symbol.Name ?? string.Empty;

            if (name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        private IReadOnlyList<TypeListEntry> BuildEntries(IReadOnlyList<LeveledSymbol> filteredSymbols)
        {
            var entries = new List<TypeListEntry>();

            if (filteredSymbols.Count == 0)
            {
                return entries;
            }

            if (navigationDirection == NavigationDirection.Base)
            {
                var classChain = filteredSymbols
                    .Where(x => x.Symbol.TypeKind == TypeKind.Class)
                    .OrderBy(x => x.Level)
                    .ThenBy(x => TypeDisplayNameProvider.GetDisplayName(x.Symbol))
                    .ToList();

                var interfaces = filteredSymbols
                    .Where(x => x.Symbol.TypeKind == TypeKind.Interface)
                    .OrderBy(x => x.Level)
                    .ThenBy(x => TypeDisplayNameProvider.GetDisplayName(x.Symbol))
                    .ToList();

                AddGroup(entries, "Base class chain", classChain);
                AddGroup(entries, "Interfaces", interfaces);

                return entries;
            }

            if (isTargetInterface)
            {
                var derivedInterfaces = filteredSymbols.Where(x => x.Symbol.TypeKind == TypeKind.Interface).ToList();
                var implementations = filteredSymbols.Where(x => x.Symbol.TypeKind != TypeKind.Interface).ToList();

                AddDirectIndirectGroups(entries, "Derived interfaces", derivedInterfaces);
                AddDirectIndirectGroups(entries, "Implementations", implementations);

                return entries;
            }

            AddDirectIndirectGroups(entries, "Derived types", filteredSymbols.ToList());
            return entries;
        }

        private static void AddDirectIndirectGroups(List<TypeListEntry> entries, string title, IReadOnlyList<LeveledSymbol> symbols)
        {
            var direct = symbols
                .Where(x => x.Level == 1)
                .OrderBy(x => TypeDisplayNameProvider.GetDisplayName(x.Symbol))
                .ToList();

            var indirect = symbols
                .Where(x => x.Level >= 2)
                .OrderBy(x => x.Level)
                .ThenBy(x => TypeDisplayNameProvider.GetDisplayName(x.Symbol))
                .ToList();

            if (direct.Count > 0)
            {
                entries.Add(new GroupHeaderEntry($"{title} · Direct"));
                entries.AddRange(direct.Select(CreateSymbolEntry));
            }

            if (indirect.Count > 0)
            {
                entries.Add(new GroupHeaderEntry($"{title} · Indirect"));
                entries.AddRange(indirect.Select(CreateSymbolEntry));
            }
        }

        private static void AddGroup(List<TypeListEntry> entries, string title, IReadOnlyList<LeveledSymbol> symbols)
        {
            if (symbols.Count == 0)
            {
                return;
            }

            entries.Add(new GroupHeaderEntry(title));
            entries.AddRange(symbols.Select(CreateSymbolEntry));
        }

        private static SymbolEntry CreateSymbolEntry(LeveledSymbol leveledSymbol)
        {
            var displayName = TypeDisplayNameProvider.GetDisplayName(leveledSymbol.Symbol);

            var kindGlyph = GetKindGlyph(leveledSymbol.Symbol);
            var details = $"L{leveledSymbol.Level}";

            var indentPixels = Math.Max(0, (leveledSymbol.Level - 1) * 14);
            var indentMargin = new Thickness(indentPixels, 0, 0, 0);

            return new SymbolEntry(leveledSymbol, displayName, details, kindGlyph, indentMargin);
        }

        private static string GetKindGlyph(INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                return "?";
            }

            if (typeSymbol.TypeKind == TypeKind.Interface)
            {
                return "I";
            }

            if (typeSymbol.TypeKind == TypeKind.Struct)
            {
                return "S";
            }

            if (typeSymbol.TypeKind == TypeKind.Enum)
            {
                return "E";
            }

            if (typeSymbol.IsRecord)
            {
                return "R";
            }

            if (typeSymbol.IsAbstract)
            {
                return "A";
            }

            return "C";
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
            SearchTextBox.Focus();
            Keyboard.Focus(SearchTextBox);

            AnimateDirectionArrowRotation();
        }

        private void AdjustWindowWidthToLongestItem(IReadOnlyList<TypeListEntry> entries)
        {
            var itemList = entries ?? Array.Empty<TypeListEntry>();

            var typeface = new Typeface(TypeListBox.FontFamily, TypeListBox.FontStyle, TypeListBox.FontWeight, TypeListBox.FontStretch);
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            double maximumTextWidth = 0.0;

            foreach (var item in itemList)
            {
                string text;

                if (item is GroupHeaderEntry groupHeader)
                {
                    text = groupHeader.Title;
                }
                else if (item is SymbolEntry symbolEntry)
                {
                    text = symbolEntry.DisplayName;
                }
                else
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var formattedText = new FormattedText(
                    text,
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

            var screenPoint = System.Windows.Forms.Cursor.Position;
            var screen = System.Windows.Forms.Screen.FromPoint(screenPoint);
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

            DirectionArrowRotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
        }

        private void HandleListBoxMouseDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArguments)
        {
            CommitSelection();
        }

        private void CommitSelection()
        {
            var selectedItem = TypeListBox.SelectedItem as SymbolEntry;
            if (selectedItem == null)
            {
                if (!TypeListBox.IsEnabled)
                {
                    DialogResult = false;
                    Close();
                }

                return;
            }

            SelectedSymbol = selectedItem.Symbol;
            DialogResult = true;
            Close();
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public abstract class TypeListEntry
        {
            public bool IsSelectable { get; }

            protected TypeListEntry(bool isSelectable)
            {
                IsSelectable = isSelectable;
            }
        }

        public sealed class GroupHeaderEntry : TypeListEntry
        {
            public string Title { get; }

            public GroupHeaderEntry(string title)
                : base(false)
            {
                Title = title ?? string.Empty;
            }
        }

        public sealed class SymbolEntry : TypeListEntry
        {
            public LeveledSymbol Symbol { get; }

            public string DisplayName { get; }

            public string Details { get; }

            public string KindGlyph { get; }

            public Thickness IndentMargin { get; }

            public SymbolEntry(
                LeveledSymbol symbol,
                string displayName,
                string details,
                string kindGlyph,
                Thickness indentMargin)
                : base(true)
            {
                Symbol = symbol;
                DisplayName = displayName ?? string.Empty;
                Details = details ?? string.Empty;
                KindGlyph = kindGlyph ?? string.Empty;
                IndentMargin = indentMargin;
            }
        }
    }
}
