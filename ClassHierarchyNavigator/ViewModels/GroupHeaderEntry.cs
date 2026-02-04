namespace ClassHierarchyNavigator.ViewModels
{
    public sealed class GroupHeaderEntry : TypeListEntry
    {
        public GroupHeaderEntry(string title) : base(false)
        {
            Title = title ?? string.Empty;
        }

        public string Title { get; }
    }
}
