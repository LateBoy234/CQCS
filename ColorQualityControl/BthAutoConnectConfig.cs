using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.ColorQualityControl
{
    public class BthAutoConnectConfig
    {
        public BthAutoConnectConfig()
        {
            IsAutoConnect = false;
            IsAutoConnectOtherInstrument = false;
        }
        public bool  IsAutoConnect { get; set; }
        public bool  IsAutoConnectOtherInstrument { get; set; }
        public string SlaveName { get; set; }
        public string SlaveMac { get; set; }
    }
}
