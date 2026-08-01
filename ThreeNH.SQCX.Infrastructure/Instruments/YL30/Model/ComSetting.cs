using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30.Model
{
    public class ComSetting
    {
        public ushort SlaveID { get; set; }
        public BaudRate BaudRate { get; set; }
        public CommInterfaceType CommInterfaceType { get; set; }

    }
}
