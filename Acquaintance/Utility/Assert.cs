using System;

namespace Acquaintance.Utility
{
    public static class Assert
    {
        public static void ArgumentNotNull(object arg, string name)
        {
            if (arg == null)
                throw new ArgumentNullException(name);
        }

        public static void IsInRange(int arg, string name, int low, int high)
        {
            if (arg < low || arg > high)
                throw new ArgumentOutOfRangeException(name, $"Argument {name}={arg} must be between {low} and {high}");
        }

        public static void IsInRange(long arg, string name, long low, long high)
        {
            if (arg < low || arg > high)
                throw new ArgumentOutOfRangeException(name, $"Argument {name}={arg} must be between {low} and {high}");
        }

        public static void IsInstanceOf(Type type, object obj, string objName)
        {
            if (!type.IsInstanceOfType(obj))
                throw new ArgumentException($"Argument {objName} is of type {obj.GetType().Name} but expected {type.Name}");
        }
    }
}
