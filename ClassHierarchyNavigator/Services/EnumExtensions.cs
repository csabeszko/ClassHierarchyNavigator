using System;
using System.ComponentModel;
using System.Reflection;

namespace ClassHierarchyNavigator.Services
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = value.ToString();
            FieldInfo field = type.GetField(name);

            if (field == null)
            {
                return name;
            }

            DescriptionAttribute attribute = field.GetCustomAttribute<DescriptionAttribute>();

            if (attribute == null)
            {
                return name;
            }

            return attribute.Description;
        }
    }
}
