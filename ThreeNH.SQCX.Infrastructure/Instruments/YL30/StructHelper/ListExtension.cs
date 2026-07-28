using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YL30.StructHelper
{
    public static  class ListExtension
    {
        public static IList<T> TakeRange<T>(this IList<T> list1, int start, int count)
        {
            return list1.Skip(start).Take(count).ToList();
        }
    }
}
