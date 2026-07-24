using System;
using System.ComponentModel;

namespace ThreeNH.ColorAlgorithm
{
    public enum StandardObserver
    {
        [Description("2\u00B0")]
        CIE1931, // CIE 1931年定义的标准观察者，即2度视角
        [Description("10\u00B0")]
        CIE1964,  // CIE 1964年定义的观察者角度，即10度视角
        [Description("")]
        Unknown
    }

    public static class StandardObserverExtension
    {
        public static StandardObserver ToStandardObserver(this string name)
        {
            StandardObserver result;
            if (Enum.TryParse(name, out result))
                return result;

            if (name == "2\u00B0" || name == "CIE1931")
                return StandardObserver.CIE1931;
            if (name == "10\u00B0" || name== "CIE1964")
                return StandardObserver.CIE1964;
            return StandardObserver.Unknown;
        }
    }
}
