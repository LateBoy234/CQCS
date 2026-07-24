using System;
using System.Xml;
using ThreeNH.Helper;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl
{
    public struct ObserverIlluminantPair
    {
        public bool Equals(ObserverIlluminantPair other)
        {
            return Observer == other.Observer && Illuminant == other.Illuminant;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ObserverIlluminantPair && Equals((ObserverIlluminantPair) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Observer * 397) ^ (int) Illuminant;
            }
        }

        public StandardObserver Observer;
        public StandardIlluminant Illuminant;

        public ObserverIlluminantPair(StandardObserver observer, StandardIlluminant illuminant)
        {
            Observer = observer;
            Illuminant = illuminant;
        }

        public static bool operator ==(ObserverIlluminantPair left, ObserverIlluminantPair right)
        {
            return left.Illuminant == right.Illuminant && left.Observer == right.Observer;
        }

        public static bool operator !=(ObserverIlluminantPair left, ObserverIlluminantPair right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", Illuminant.ToString(), Observer.Description());
        }

        public bool IsValid
        {
            get { return Observer != StandardObserver.Unknown && Illuminant != StandardIlluminant.Unknown; }
        }
    }

    // 观察者角度和光源设置
    public class ObserverAndIlluminantSetting
    {
        public ObserverIlluminantPair Primary;
        public ObserverIlluminantPair Secondary;
        public ObserverIlluminantPair Tertiary;

        public StandardObserver Observer
        {
            get { return Primary.Observer; }
            set { Primary.Observer = value; }
        }

        public StandardIlluminant PrimaryIlluminant
        {
            get { return Primary.Illuminant; }
            set { Primary.Illuminant = value; }
        }

        public StandardObserver PrimaryObserver
        {
            get { return Primary.Observer; }
            set { Primary.Observer = value; }
        }

        public StandardIlluminant SecondaryIlluminant
        {
            get { return Secondary.Illuminant; }
            set { Secondary.Illuminant = value; }
        }

        public StandardObserver SecondaryObserver
        {
            get { return Secondary.Observer; }
            set { Secondary.Observer = value; }
        }

        public StandardIlluminant TertiaryIlluminant
        {
            get { return Tertiary.Illuminant; }
            set { Tertiary.Illuminant = value; }
        }

        public StandardObserver TertiaryObserver
        {
            get { return Tertiary.Observer; }
            set { Tertiary.Observer = value; }
        }

        public override bool Equals(object obj)
        {
            if (obj is ObserverAndIlluminantSetting)
            {
                var other = (ObserverAndIlluminantSetting)obj;
                return this.Primary == other.Primary &&
                       this.Secondary == other.Secondary &&
                       this.Tertiary == other.Tertiary;
            }
            throw new ArgumentException(string.Format("Can't compare between {0} and {1}",
                typeof(ObserverAndIlluminantSetting), obj.GetType()));
        }

        public override int GetHashCode()
        {
            return ((int)PrimaryObserver << 24) | ((int)SecondaryObserver << 26) | ((int)TertiaryObserver << 28) |
                   ((int)PrimaryIlluminant << 16) | ((int)SecondaryIlluminant << 8) | (int)TertiaryIlluminant;
        }
        public XmlElement ToXmlElement(XmlDocument doc, string nodeName)
        {
            var node = doc.CreateElement(nodeName);
            node.SetAttribute("Version", "2");
            node.SetAttribute(nameof(PrimaryObserver), PrimaryObserver.ToString());
            node.SetAttribute(nameof(SecondaryObserver), SecondaryObserver.ToString());
            node.SetAttribute(nameof(TertiaryObserver), TertiaryObserver.ToString());
            node.SetAttribute("PrimaryIlluminant", PrimaryIlluminant.ToString());
            node.SetAttribute("SecondaryIlluminant", SecondaryIlluminant.ToString());
            node.SetAttribute("TertiaryIlluminant", TertiaryIlluminant.ToString());

            return node;
        }
        public bool LoadXmlElement(XmlElement node)
        {
            if (node == null)
                return false;

            if (node.GetAttribute("Version") == "2")
            {
                try
                {
                    PrimaryObserver = (StandardObserver) Enum.Parse(typeof(StandardObserver), node.GetAttribute(nameof(PrimaryObserver)));
                }
                catch
                {
                    PrimaryObserver = StandardObserver.CIE1964;
                }

                try
                {
                    SecondaryObserver = (StandardObserver) Enum.Parse(typeof(StandardObserver), node.GetAttribute(nameof(SecondaryObserver)));
                }
                catch
                {
                    SecondaryObserver = StandardObserver.CIE1964;
                }

                try
                {
                    TertiaryObserver = (StandardObserver) Enum.Parse(typeof(StandardObserver), node.GetAttribute(nameof(TertiaryObserver)));
                }
                catch
                {
                    TertiaryObserver = StandardObserver.CIE1964;
                }
            }
            else
            {
                StandardObserver observer;
                try
                {
                    observer = (StandardObserver) Enum.Parse(typeof(StandardObserver), node.GetAttribute("Observer"));
                }
                catch
                {
                    observer = StandardObserver.CIE1964;
                }

                PrimaryObserver = SecondaryObserver = TertiaryObserver = observer;
            }

            try
            {
                PrimaryIlluminant = (StandardIlluminant) Enum.Parse(typeof(StandardIlluminant),
                    node.GetAttribute(nameof(PrimaryIlluminant)));
            }
            catch
            {
                PrimaryIlluminant = StandardIlluminant.D65;
            }

            try
            {
                SecondaryIlluminant = (StandardIlluminant) Enum.Parse(typeof(StandardIlluminant),
                    node.GetAttribute(nameof(SecondaryIlluminant)));
            }
            catch
            {
                SecondaryIlluminant = StandardIlluminant.D65;
            }

            try
            {
                TertiaryIlluminant = (StandardIlluminant) Enum.Parse(typeof(StandardIlluminant),
                    node.GetAttribute(nameof(TertiaryIlluminant)));
            }
            catch
            {
                TertiaryIlluminant = StandardIlluminant.D65;
            }

            return true;
        }
    }


}
