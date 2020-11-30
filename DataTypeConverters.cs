using System;

namespace AnyBase
{
    internal static class DataTypeConverters
    {
        internal static object ToTimespan(object ticks)
        {
            return new TimeSpan((long)ticks);
        }

        internal static object ToTicks(object span)
        {
            return ((TimeSpan)span).Ticks;
        }

        internal static object ToUInt64FromDecimal(object number)
        {
            return Convert.ToInt64(number);
        }
        
        internal static object ToStringFromNullableGuid(object guid)
        {
            return guid?.ToString();
        }
        
        internal static object ToInt32FromInt64(object number)
        {
            return Convert.ToInt32(number);
        }

        internal static object ToInt64FromUInt32(object number)
        {
            return Convert.ToInt64(number);
        }
        
        internal static object ToDecimalFromUInt64(object number)
        {
            return Convert.ToDecimal(number);
        }
        
        internal static object ToBoolean(this object number)
        {
            return Convert.ToBoolean(number);
        }
    }
}