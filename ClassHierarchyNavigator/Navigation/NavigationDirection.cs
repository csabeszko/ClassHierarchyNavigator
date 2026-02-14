using System.ComponentModel;

namespace ClassHierarchyNavigator.Navigation
{
    public enum NavigationDirection
    {
        [Description("Navigate to base class")]
        Base,
        [Description("Navigate to derived class")]
        Derived
    }
}
