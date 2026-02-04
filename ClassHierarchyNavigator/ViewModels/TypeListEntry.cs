namespace ClassHierarchyNavigator.ViewModels
{
    public abstract class TypeListEntry
    {
        protected TypeListEntry(bool isSelectable)
        {
            IsSelectable = isSelectable;
        }

        public bool IsSelectable { get; }
    }
}
