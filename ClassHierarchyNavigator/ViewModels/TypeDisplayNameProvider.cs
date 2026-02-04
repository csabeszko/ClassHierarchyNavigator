using Microsoft.CodeAnalysis;

namespace ClassHierarchyNavigator.ViewModels
{
    public static class TypeDisplayNameProvider
    {
        public static string GetDisplayName(INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                return string.Empty;
            }

            return typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }
    }
}
