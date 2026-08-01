using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30.Model
{
    public class Specimen
    {
        public Specimen() 
        {
            
        }
        public MeasureMode Mode { get; set; }
        public SpecularComponent SCMode { get; set; }

        public UVMode UVMode { get; set; }
        public Caliber Caliber { get; set; }
        public LensPos LensPos { get; set; }    

        public DataType  DataType { get; set; }
        public ColorSpace ColorSpace { get; set; }
        public lockStatus LockStatus { get; set; }
        public  StandardObserver StandardObserver { get; set; }
        public  StandardIlluminant illuminant { get; set; }
        public  Tolerance Tolerance { get; set; }
        public  string Name {  get; set; }
        public List<ushort> ReflectanceSCI { get; set; }
        public List<ushort> ReflectanceSCE { get; set; }

        public string ColorSpaceValue { get; set; }
        public ushort ID;
        public System.DateTime DateTime { get;set; }




    }
}
