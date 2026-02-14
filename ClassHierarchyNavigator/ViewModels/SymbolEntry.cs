using ClassHierarchyNavigator.Models;
using Microsoft.CodeAnalysis;
using System;
using System.Windows;

namespace ClassHierarchyNavigator.ViewModels
{
    public sealed class SymbolEntry : TypeListEntry
    {
        private SymbolEntry(
          LeveledSymbol symbol,
          string displayName,
          string details,
          string kindGlyph,
          Thickness indentMargin) : base(true)
        {
            Symbol = symbol;
            DisplayName = displayName ?? string.Empty;
            Details = details ?? string.Empty;
            KindGlyph = kindGlyph ?? "?";
            IndentMargin = indentMargin;
        }

        public LeveledSymbol Symbol { get; }

        public string DisplayName { get; }

        public string Details { get; }

        public string KindGlyph { get; }

        public Thickness IndentMargin { get; }

        public static SymbolEntry Create(LeveledSymbol leveledSymbol)
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
    }
}
