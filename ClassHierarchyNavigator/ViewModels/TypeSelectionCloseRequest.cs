using ClassHierarchyNavigator.Models;

namespace ClassHierarchyNavigator.ViewModels
{
    public sealed class TypeSelectionCloseRequest
    {
        private TypeSelectionCloseRequest(bool dialogResult, LeveledSymbol? selectedSymbol)
        {
            DialogResult = dialogResult;
            SelectedSymbol = selectedSymbol;
        }

        public bool DialogResult { get; }
        public LeveledSymbol? SelectedSymbol { get; }

        public static TypeSelectionCloseRequest Accept(LeveledSymbol selectedSymbol)
        {
            return new TypeSelectionCloseRequest(true, selectedSymbol);
        }

        public static TypeSelectionCloseRequest Cancel()
        {
            return new TypeSelectionCloseRequest(false, null);
        }
    }
}
