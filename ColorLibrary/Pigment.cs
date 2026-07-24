using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorLibrary
{
    /// <summary>
    /// 颜料
    /// </summary>
    public class Pigment
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime DateTime { get; set; } = DateTime.Now;
        public string Remark { get; set; }
    }
}
