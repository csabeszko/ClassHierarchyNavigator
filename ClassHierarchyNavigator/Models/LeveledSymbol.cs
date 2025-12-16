using Microsoft.CodeAnalysis;

namespace ClassHierarchyNavigator.Models
{
    public class LeveledSymbol
    {
        private readonly INamedTypeSymbol _symbol;
        private readonly int _level;

        public LeveledSymbol(INamedTypeSymbol symbol, int level)
        {
            _symbol = symbol;
            _level = level;
        }

        public INamedTypeSymbol Symbol => _symbol;

        public int Level => _level;
    }
}
