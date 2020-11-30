using System;
using System.Collections.Generic;

namespace AnyBase
{
    internal static class DataTypeRestrictions
    {
        private static readonly Dictionary<string, Tuple<object, object>> RestrictionsByDescriptor = new Dictionary<string, Tuple<object, object>>
        {
            { "DateTime", new Tuple<object, object>(-9999999999.999999999999999999D, 9999999999.999999999999999999D) },
            { "Decimal", new Tuple<object, object>(new DateTime(1753, 1, 1), new DateTime(9999, 31, 12, 23, 59, 59))},
            { "Single", new Tuple<object, object>(-1.0E+23F, 1.0E+23F)},
            { "TimeSpan", new Tuple<object, object>(new TimeSpan(-838, -59, -59), new TimeSpan(838, 59, 59))}
        };

        internal static bool IsWithinBounds(string dataType, object value)
        {
            var (lowerBound, upperBound) = RestrictionsByDescriptor[dataType];
            switch (dataType)
            {
                case "DateTime":
                    return (DateTime) value >= (DateTime) lowerBound &&
                           (DateTime) value <= (DateTime) upperBound;
                case "Decimal":
                    return (decimal) value >= (decimal) lowerBound &&
                           (decimal) value <= (decimal) upperBound;
                case "Single":
                    return (float) value >= (float) lowerBound &&
                           (float) value <= (float) upperBound;
                case "Double":
                    return (double) value >= (double) lowerBound &&
                           (double) value <= (double) upperBound;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType));
            }
        }
    }
}