using System;
using System.Collections.Generic;
using System.Linq;

namespace ZAMERT
{
    public static class Extensions
    {
        public static IEnumerable<Enum> GetFlags(this Enum value)
            => Enum.GetValues(value.GetType()).Cast<Enum>().ToArray();

        public static IEnumerable<Enum> GetActiveFlags(this Enum value)
        {
            var result = new List<Enum>();
            foreach (var flag in value.GetFlags())
                if (value.HasFlag(flag)) result.Add(flag);
            return result;
        }

        public static string GetActiveFlagNames(this Enum value)
            => string.Join(", ", value.GetActiveFlags());
    }
}
