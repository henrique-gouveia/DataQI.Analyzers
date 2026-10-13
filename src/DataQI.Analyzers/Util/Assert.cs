using System;

namespace DataQI.Analyzers.Util
{
    public static class Assert
    {
        public static void NotNull(object value, string message)
        {
            if (value == null)
                throw new ArgumentException(message);
        }

        public static void NotNullOrEmpty(string value, string message)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException(message);
        }
    }
}
