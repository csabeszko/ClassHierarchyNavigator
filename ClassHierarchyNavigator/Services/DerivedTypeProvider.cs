using ClassHierarchyNavigator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using System.Collections.Generic;
using System.Linq;
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
            var visitedSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            await CollectTransitiveDerivedTypesAsync(
                0,
                leveledSymbols,
                visitedSymbols,
                baseTypeSymbol,
                solution,
                cancellationToken);

            return leveledSymbols;
        }

        private static async Task CollectTransitiveDerivedTypesAsync(
            int level,
            IList<LeveledSymbol> leveledSymbols,
            ISet<ISymbol> visitedSymbols,
            INamedTypeSymbol baseTypeSymbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var nextLevel = level + 1;

            if (baseTypeSymbol.TypeKind == TypeKind.Interface)
            {
                var derivedInterfaces = await SymbolFinder.FindDerivedInterfacesAsync(
                    baseTypeSymbol,
                    solution,
                    transitive: false,
                    cancellationToken: cancellationToken);

                foreach (var derivedInterface in derivedInterfaces)
                {
                    if (visitedSymbols.Add(derivedInterface))
                    {
                        leveledSymbols.Add(new LeveledSymbol(derivedInterface, nextLevel));
                        await CollectTransitiveDerivedTypesAsync(
                            nextLevel,
                            leveledSymbols,
                            visitedSymbols,
                            derivedInterface,
                            solution,
                            cancellationToken);
                    }
                }

                var implementationSymbols = await SymbolFinder.FindImplementationsAsync(
                    baseTypeSymbol,
                    solution,
                    transitive: false,
                    projects: null,
                    cancellationToken: cancellationToken);

                foreach (var implementationSymbol in implementationSymbols.OfType<INamedTypeSymbol>())
                {
                    if (visitedSymbols.Add(implementationSymbol))
                    {
                        leveledSymbols.Add(new LeveledSymbol(implementationSymbol, nextLevel));
                        await CollectTransitiveDerivedTypesAsync(
                            nextLevel,
                            leveledSymbols,
                            visitedSymbols,
                            implementationSymbol,
                            solution,
                            cancellationToken);
                    }
                }

                return;
            }

            var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(
                baseTypeSymbol,
                solution,
                transitive: false,
                projects: null,
                cancellationToken: cancellationToken);

            foreach (var derivedClass in derivedClasses)
            {
                if (visitedSymbols.Add(derivedClass))
                {
                    leveledSymbols.Add(new LeveledSymbol(derivedClass, nextLevel));
                    await CollectTransitiveDerivedTypesAsync(
                        nextLevel,
                        leveledSymbols,
                        visitedSymbols,
                        derivedClass,
                        solution,
                        cancellationToken);
                }
            }
        }
    }
}
