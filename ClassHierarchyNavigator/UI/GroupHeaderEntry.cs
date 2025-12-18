namespace ClassHierarchyNavigator.UI
{
    public partial class TypeSelectionWindow
    {
        public sealed class GroupHeaderEntry : TypeListEntry
        {
            public string Title { get; }

            public GroupHeaderEntry(string title)
                : base(false)
            {
                Title = title ?? string.Empty;
            }
        }
    }
}
