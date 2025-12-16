using ClassHierarchyNavigator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClassHierarchyNavigator.Services
{
    public static class DerivedTypeProvider
    {
        public static async Task<IReadOnlyList<LeveledSymbol>> GetDerivedTypesAsync(
            INamedTypeSymbol baseTypeSymbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var leveledSymbols = new List<LeveledSymbol>();

            await FindTransitiveImplementationsAsync(0, leveledSymbols, baseTypeSymbol, solution, cancellationToken);

            return leveledSymbols;
        }

        private static async Task FindTransitiveImplementationsAsync(
            int level,
            IList<LeveledSymbol> leveledSymbols,
            INamedTypeSymbol baseTypeSymbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            level++;

            var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(
                baseTypeSymbol,
                solution,
                transitive: false,
                projects: null,
                cancellationToken: cancellationToken);

            foreach (var derivedClass in derivedClasses)
            {
                leveledSymbols.Add(new LeveledSymbol(derivedClass, level));
                await FindTransitiveImplementationsAsync(level, leveledSymbols, derivedClass, solution, cancellationToken);
            }

            if (baseTypeSymbol.TypeKind == TypeKind.Interface)
            {
                var implementations = await SymbolFinder.FindImplementationsAsync(
                    baseTypeSymbol,
                    solution,
                    transitive: false,
                    projects: null,
                    cancellationToken: cancellationToken);

                foreach (var implementation in implementations)
                {
                    if (implementation is INamedTypeSymbol namedTypeSymbol)
                    {
                        leveledSymbols.Add(new LeveledSymbol(namedTypeSymbol, level));
                        await FindTransitiveImplementationsAsync(level, leveledSymbols, namedTypeSymbol, solution, cancellationToken);
                    }
                }
            }
        }
    }
}
