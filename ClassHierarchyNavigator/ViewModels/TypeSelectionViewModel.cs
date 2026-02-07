using ClassHierarchyNavigator.Models;
using ClassHierarchyNavigator.Navigation;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClassHierarchyNavigator.ViewModels
{
    public sealed class TypeSelectionViewModel : INotifyPropertyChanged
    {
        private static readonly Uri BaseArrowUri = new Uri("pack://application:,,,/ClassHierarchyNavigator;component/Resources/up16.png", UriKind.Absolute);
        private static readonly Uri DerivedArrowUri = new Uri("pack://application:,,,/ClassHierarchyNavigator;component/Resources/down16.png", UriKind.Absolute);

        private readonly NavigationDirection _navigationDirection;
        private readonly IReadOnlyList<LeveledSymbol> _allCandidateSymbols;
        private readonly bool _isTargetInterface;

        private TypeListEntry? _selectedEntry;
        private string _searchText;
        private string _statusText;
        private string _warningText;
        private double _windowWidth;

        public TypeSelectionViewModel(
          IReadOnlyList<LeveledSymbol> candidateSymbols,
          NavigationDirection direction,
          INamedTypeSymbol targetTypeSymbol,
          string? warningText,
          string? statusText)
        {
            _navigationDirection = direction;
            _allCandidateSymbols = candidateSymbols ?? Array.Empty<LeveledSymbol>();
            _isTargetInterface = targetTypeSymbol.TypeKind == TypeKind.Interface;

            var targetTypeDisplayName = TypeDisplayNameProvider.GetDisplayName(targetTypeSymbol);

            HeaderText = direction == NavigationDirection.Base
              ? $"Find base types of {targetTypeDisplayName}"
              : $"Find derived types of {targetTypeDisplayName}";

            DirectionArrowSource = new BitmapImage(direction == NavigationDirection.Base ? BaseArrowUri : DerivedArrowUri);

            _searchText = string.Empty;
            _warningText = warningText ?? string.Empty;
            _statusText = statusText ?? string.Empty;

            Entries = new ObservableCollection<TypeListEntry>();

            AcceptCommand = new DelegateCommand(HandleAccept);
            CancelCommand = new DelegateCommand(HandleCancel);
            MoveSelectionUpCommand = new DelegateCommand(() => MoveSelection(-1));
            MoveSelectionDownCommand = new DelegateCommand(() => MoveSelection(1));
            PreviewKeyDownCommand = new DelegateCommandWithParameter(HandlePreviewKeyDown, CanHandlePreviewKeyDown);

            _windowWidth = 300.0;

            RebuildEntries();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<TypeSelectionCloseRequest>? RequestClose;

        public ObservableCollection<TypeListEntry> Entries { get; }

        public DelegateCommand AcceptCommand { get; }

        public DelegateCommand CancelCommand { get; }

        public DelegateCommand MoveSelectionUpCommand { get; }

        public DelegateCommand MoveSelectionDownCommand { get; }

        public DelegateCommandWithParameter PreviewKeyDownCommand { get; }

        public string HeaderText { get; }

        public ImageSource DirectionArrowSource { get; }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                var normalizedValue = value ?? string.Empty;
                if (string.Equals(_searchText, normalizedValue, StringComparison.Ordinal))
                {
                    return;
                }

                _searchText = normalizedValue;
                OnPropertyChanged();
                RebuildEntries();
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            private set
            {
                var normalizedValue = value ?? string.Empty;
                if (string.Equals(_statusText, normalizedValue, StringComparison.Ordinal))
                {
                    return;
                }

                _statusText = normalizedValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasStatus));
            }
        }

        public bool HasStatus => !string.IsNullOrWhiteSpace(_statusText);

        public string WarningText
        {
            get { return _warningText; }
            private set
            {
                var normalizedValue = value ?? string.Empty;
                if (string.Equals(_warningText, normalizedValue, StringComparison.Ordinal))
                {
                    return;
                }

                _warningText = normalizedValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasWarning));
            }
        }

        public bool HasWarning => !string.IsNullOrWhiteSpace(_warningText);

        public bool HasSelectableEntries => Entries.OfType<SymbolEntry>().Any();

        public TypeListEntry? SelectedEntry
        {
            get { return _selectedEntry; }
            set
            {
                if (ReferenceEquals(_selectedEntry, value))
                {
                    return;
                }

                _selectedEntry = value;
                OnPropertyChanged();
            }
        }

        public double WindowWidth
        {
            get { return _windowWidth; }
            set
            {
                if (Math.Abs(_windowWidth - value) < 0.5)
                {
                    return;
                }

                _windowWidth = value;
                OnPropertyChanged();
            }
        }

        private void HandleAccept()
        {
            var symbolEntry = SelectedEntry as SymbolEntry;
            if (symbolEntry == null)
            {
                if (!HasSelectableEntries)
                {
                    RequestClose?.Invoke(this, TypeSelectionCloseRequest.Cancel());
                }

                return;
            }

            RequestClose?.Invoke(this, TypeSelectionCloseRequest.Accept(symbolEntry.Symbol));
        }

        private void HandleCancel()
        {
            RequestClose?.Invoke(this, TypeSelectionCloseRequest.Cancel());
        }

        private void RebuildEntries()
        {
            var normalizedSearch = (_searchText ?? string.Empty).Trim();

            IEnumerable<LeveledSymbol> filtered = _allCandidateSymbols;

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                filtered = filtered.Where(symbol => MatchesFilter(symbol, normalizedSearch));
            }

            var filteredList = filtered.ToList();
            var rebuilt = BuildEntries(filteredList);

            Entries.Clear();
            foreach (var entry in rebuilt)
            {
                Entries.Add(entry);
            }

            var firstSelectable = Entries.OfType<SymbolEntry>().FirstOrDefault();
            if (firstSelectable != null)
            {
                SelectedEntry = firstSelectable;
                StatusText = string.Empty;
            }
            else
            {
                SelectedEntry = null;
                StatusText = _allCandidateSymbols.Count == 0 ? "No results." : "No results (after filtering).";
            }

            OnPropertyChanged(nameof(HasSelectableEntries));
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

            if (_navigationDirection == NavigationDirection.Base)
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

            if (_isTargetInterface)
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

        private void MoveSelection(int delta)
        {
            var selectableEntries = Entries.OfType<SymbolEntry>().ToList();
            if (selectableEntries.Count == 0)
            {
                return;
            }

            var current = SelectedEntry as SymbolEntry;
            var currentIndex = current != null ? selectableEntries.IndexOf(current) : -1;

            var nextIndex = currentIndex + delta;

            if (currentIndex < 0)
            {
                nextIndex = delta > 0 ? 0 : selectableEntries.Count - 1;
            }

            if (nextIndex < 0)
            {
                nextIndex = 0;
            }

            if (nextIndex >= selectableEntries.Count)
            {
                nextIndex = selectableEntries.Count - 1;
            }

            SelectedEntry = selectableEntries[nextIndex];
        }

        private bool CanHandlePreviewKeyDown(object? parameter)
        {
            if (parameter is not KeyEventArgs keyEventArguments)
            {
                return false;
            }

            if (keyEventArguments.Key == Key.Up || keyEventArguments.Key == Key.Down)
            {
                return true;
            }

            return false;
        }

        private void HandlePreviewKeyDown(object? parameter)
        {
            if (parameter is not KeyEventArgs keyEventArguments)
            {
                return;
            }

            if (keyEventArguments.Key == Key.Up)
            {
                MoveSelection(-1);
                return;
            }

            if (keyEventArguments.Key == Key.Down)
            {
                MoveSelection(1);
                return;
            }
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
                entries.AddRange(direct.Select(SymbolEntry.Create));
            }

            if (indirect.Count > 0)
            {
                entries.Add(new GroupHeaderEntry($"{title} · Indirect"));
                entries.AddRange(indirect.Select(SymbolEntry.Create));
            }
        }

        private static void AddGroup(List<TypeListEntry> entries, string title, IReadOnlyList<LeveledSymbol> symbols)
        {
            if (symbols.Count == 0)
            {
                return;
            }

            entries.Add(new GroupHeaderEntry(title));
            entries.AddRange(symbols.Select(SymbolEntry.Create));
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
