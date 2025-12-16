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

            GetTransitiveBaseTypes(0, currentType, result);

            return result;
        }

        private static void GetTransitiveBaseTypes(
            int level, 
            INamedTypeSymbol currentType, 
            IList<LeveledSymbol> leveledSymbols)
        {
            level++;

            var baseType = currentType.BaseType;
            if (baseType != null && baseType.SpecialType != SpecialType.System_Object)
            {
                leveledSymbols.Add(new LeveledSymbol(baseType, level));
                GetTransitiveBaseTypes(level, baseType, leveledSymbols);
            }

            foreach (var interfaceType in currentType.Interfaces)
            {
                leveledSymbols.Add(new LeveledSymbol(interfaceType, level));
                GetTransitiveBaseTypes(level, interfaceType, leveledSymbols);
            }
        }
    }
}
