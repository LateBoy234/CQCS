using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl
{
    public class StandardWrap : SpecimenWrap
    {
        public StandardWrap()
        {
        }

        public StandardWrap(StandardSpecimenAdapter adapter)
            : base(adapter)
        {
        }

        ObservableCollection<SpecimenWrap> _testWraps = new ObservableCollection<SpecimenWrap>();
        public ObservableCollection<SpecimenWrap> TestWraps
        {
            get { return _testWraps; }
        }

        public StandardSpecimen StandardSpecimen
        {
            get { return Specimen as StandardSpecimen; }
        }

        public StandardSpecimenAdapter StandardAdapter
        {
            get { return Adapter as StandardSpecimenAdapter; }
        }
    }
}
