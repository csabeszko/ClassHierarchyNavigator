namespace ClassHierarchyNavigator.UI
{
    public partial class TypeSelectionWindow
    {
        public abstract class TypeListEntry
        {
            public bool IsSelectable { get; }

            protected TypeListEntry(bool isSelectable)
            {
                IsSelectable = isSelectable;
            }
        }
    }
}
