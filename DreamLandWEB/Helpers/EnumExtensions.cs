using System.ComponentModel.DataAnnotations;

namespace DreamLandWEB.Helpers
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DisplayAttribute), false)
                                  .FirstOrDefault() as DisplayAttribute;
            return attribute?.Name ?? value.ToString();
        }
    }
}
