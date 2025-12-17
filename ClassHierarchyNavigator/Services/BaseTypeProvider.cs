using ClassHierarchyNavigator.Models;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace ClassHierarchyNavigator.Services
{
    public static class BaseTypeProvider
    {
        public static IReadOnlyList<LeveledSymbol> GetBaseTypes(INamedTypeSymbol currentType)
        {
            var result = new List<LeveledSymbol>();
            var visitedSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            CollectTransitiveBaseTypes(
                0,
                currentType,
                result,
                visitedSymbols);

            return result;
        }

        private static void CollectTransitiveBaseTypes(
            int level,
            INamedTypeSymbol currentType,
            IList<LeveledSymbol> leveledSymbols,
            ISet<ISymbol> visitedSymbols)
        {
            var nextLevel = level + 1;

            if (currentType.TypeKind != TypeKind.Interface)
            {
                var baseType = currentType.BaseType;
                if (baseType != null && baseType.SpecialType != SpecialType.System_Object)
                {
                    if (visitedSymbols.Add(baseType))
                    {
                        leveledSymbols.Add(new LeveledSymbol(baseType, nextLevel));
                    }

                    CollectTransitiveBaseTypes(
                        nextLevel,
                        baseType,
                        leveledSymbols,
                        visitedSymbols);
                }
            }

            foreach (var interfaceType in currentType.Interfaces)
            {
                if (visitedSymbols.Add(interfaceType))
                {
                    leveledSymbols.Add(new LeveledSymbol(interfaceType, nextLevel));
                }

                CollectTransitiveBaseTypes(
                    nextLevel,
                    interfaceType,
                    leveledSymbols,
                    visitedSymbols);
            }

            if (currentType.TypeKind == TypeKind.Interface)
            {
                foreach (var baseInterfaceType in currentType.Interfaces)
                {
                    if (visitedSymbols.Add(baseInterfaceType))
                    {
                        leveledSymbols.Add(new LeveledSymbol(baseInterfaceType, nextLevel));
                    }

                    CollectTransitiveBaseTypes(
                        nextLevel,
                        baseInterfaceType,
                        leveledSymbols,
                        visitedSymbols);
                }
            }
        }
    }
}
