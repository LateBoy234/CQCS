using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.UI
{
    class DeltaPoint : IDeltaPlotPoint
    {
        protected bool _isSelected;
        protected JudgementLevel _judgement;
        protected double _x;
        protected double _y;
        protected double _z;
        //protected string _name;
        protected TestSpecimen _test;

        public Brush StrokeLilin { get; }
        protected DeltaPoint(TestSpecimenAdapter test)
        {
            _test = test.TestSpecimen;
            _judgement = test.Judgement == null ? JudgementLevel.Invalid : test.Judgement.TotalJudgement;
            _isSelected = false;
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    PointChanged?.Invoke();
                }
            }
        }

        public JudgementLevel Judgement
        {
            get { return _judgement; }
            set
            {
                if (_judgement != value)
                {
                    _judgement = value;
                    PointChanged?.Invoke();
                }
            }
        }

        public TestSpecimen Test
        {
            get { return _test; }
            set
            {
                _test = value;
                PointChanged?.Invoke();
            }
        }

        public string Name
        {
            get { return _test == null ? null : _test.Name; }
        }

        public double X
        {
            get
            {
                return _x;
            }
            set
            {
                if (_x != value)
                {
                    _x = value;
                    PointChanged?.Invoke();
                }
            }
        }

        public double Y
        {
            get
            {
                return _y;
            }
            set
            {
                if (_y != value)
                {
                    _y = value;
                    PointChanged?.Invoke();
                }
            }
        }

        public double Z
        {
            get
            {
                return _z;
            }
            set
            {
                if (_z != value)
                {
                    _z = value;
                    PointChanged?.Invoke();
                }
            }
        }

        public event Action PointChanged;

        public bool Equals(IDeltaPlotPoint other)
        {
            DeltaPoint otherPoint = other as DeltaPoint;
            if (otherPoint != null)
            {
                return Test == otherPoint.Test;
            }
            return false;
        }
    }

    class DeltaLabPoint : DeltaPoint
    {
        public DeltaLabPoint(TestSpecimenAdapter test)
            : base(test)
        {
            _x = test.da;
            _y = test.db;
            _z = test.dL;
        }
    }

    class DeltaLChPoint : DeltaPoint
    {
        public DeltaLChPoint(TestSpecimenAdapter test)
            : base(test)
        {
            _x = test.dC;
            _y = test.dH;
            _z = test.dL;
        }
    }

    class DeltaLuvPoint : DeltaPoint
    {
        public DeltaLuvPoint(TestSpecimenAdapter test)
             : base(test)
        {
            _x = test.du;
            _y = test.dv;
            _z = test.dL;
        }
    }

    class DeltaYxyPoint : DeltaPoint
    {
        public DeltaYxyPoint(TestSpecimenAdapter test)
            : base(test)
        {
            _x = test.dx;
            _y = test.dy;
            _z = test.dY;
        }
    }

    class DeltaHunterLabPoint : DeltaPoint
    {
        public DeltaHunterLabPoint(TestSpecimenAdapter test)
            : base(test)
        {
            _x = test.da;
            _y = test.db;
            _z = test.dL;
        }
    }
}
