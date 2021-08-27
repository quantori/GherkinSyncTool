using System;
using System.ComponentModel;

namespace TestRail.Utils
{
    /// <summary>
    /// Extension class for enums.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Returns the string assigned to the enum.
        /// </summary>
        /// <param name="enumValue">The enum to get the string value for</param>
        /// <returns></returns>
        public static string GetStringValue(this Enum enumValue)
        {
            var attributes = (DescriptionAttribute[])enumValue
                .GetType()
                .GetField(enumValue.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : string.Empty;
        }
    }
}
