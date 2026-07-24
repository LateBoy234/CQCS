using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.ColorAlgorithm.IData;

namespace ThreeNH.ColorAlgorithm.Data
{
    public class Standard : Sample, IStandard
    {
        public ITolerance[] Tolerances { get; set; }

        public Standard()
        {

        }

        public Standard(ISample sample, bool deepCopy = true)
        {
            if (deepCopy)
            {
                DeepCopyFrom(sample);
            }
            else
            {
                ShallowCopyFrom(sample);
            }
        }

        protected new void DeepCopyFrom(ISample source)
        {
            base.DeepCopyFrom(source);
            if (source is IStandard standard)
            {
                Tolerances = standard.Tolerances?.Select(x => x.Clone() as ITolerance).ToArray();
            }
        }

        protected new void ShallowCopyFrom(ISample source)
        {
            base.ShallowCopyFrom(source);
            if (source is IStandard standard)
            {
                Tolerances = standard.Tolerances;
            }
        }

        public override object Clone()
        {
            return new Standard(this);
        }
        public new bool EqualObj(object obj1, object obj2)
        {
            return base.EqualObj(obj1, obj2);
        }
    }
}
