using ClassHierarchyNavigator.Models;
using System.Windows;

namespace ClassHierarchyNavigator.UI
{
    public partial class TypeSelectionWindow
    {
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
