using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.Helper
{
    public static partial class Extension
    {
        public static void AddIfNotExist<T>(this ICollection<T> collection, T value, Func<T, bool> existFunc = null)
        {
            bool exists = existFunc == null ? collection.Contains(value) : existFunc(value);
            if (!exists)
            {
                collection.Add(value);
            }
        }
    }
}
