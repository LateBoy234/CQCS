using System;
using System.Runtime.Serialization;

namespace ThreeNH.ColorAlgorithm
{
    [DataContract]
    public struct WavelengthRange
    {
        [DataMember(Order = 0)]
        public int Min;
        [DataMember(Order = 1)]
        public int Max;
        [DataMember(Order = 2)]
        public int Step;

        public WavelengthRange(int min = 380, int max = 740, int step = 10)
        {
            Min = min;
            Max = max;
            Step = step;
        }

        public bool IsValid
        {
            get { return Min > 0 && Max > 0 && (Min < Max) && (Max - Min) % Step == 0; }
        }

        public int Count
        {
            get { return IsValid ? (Max - Min) / Step + 1 : 0; }
        }

        public bool Contains(int wavelength)
        {
            return (IsValid && wavelength >= Min &&
                            wavelength <= Max && (wavelength - Min) % Step == 0);
        }

        public bool Contains(WavelengthRange range)
        {
            return (Min <= range.Min) && (Max >= range.Max)
               && (range.Step % Step == 0) && ((range.Min - Min) % Step == 0);
        }

        public int IndexOf(int wavelength)
        {
            if (Contains(wavelength))
            {
                return (wavelength - Min) / Step;
            }
            return -1;
        }

        public static bool operator==(WavelengthRange left, WavelengthRange right)
        {
            return left.Min == right.Min && left.Max == right.Max
                && left.Step == right.Step;
        }

        public static bool operator!=(WavelengthRange left, WavelengthRange right)
        {
            return left.Min != right.Min || left.Max != right.Max ||
                left.Step != right.Step;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public int[] Wavelengths
        {
            get
            {
                int[] wavelengths = new int[Count];
                for (int wavelength = Min, i = 0; wavelength <= Max; wavelength += Step, i++)
                {
                    wavelengths[i] = wavelength;
                }
                return wavelengths;
            }
        }

        public int this[int index]
        {
            get { return Min + index * Step; }
        }

        public static WavelengthRange Union(WavelengthRange left, WavelengthRange right)
        {
            if (left.Step % right.Step != 0 && right.Step % left.Step != 0)
            {
                throw new ArgumentException("left.Step and right.Step are not compatible");
            }

            int min = Math.Min(left.Min, right.Min);
            int max = Math.Max(left.Max, right.Max);
            int step = Math.Min(left.Step, right.Step);
            return new WavelengthRange(min, max, step);
        }

        public static WavelengthRange Intersect(WavelengthRange left, WavelengthRange right)
        {
            if (left.Step % right.Step != 0 && right.Step % left.Step != 0)
            {
                throw new ArgumentException("left.Step and right.Step are not compatible");
            }

            int min = Math.Max(left.Min, right.Max);
            int max = Math.Min(left.Max, right.Max);
            int step = Math.Max(left.Step, right.Step);
            return new WavelengthRange(min, max, step);
        }
    }
}
