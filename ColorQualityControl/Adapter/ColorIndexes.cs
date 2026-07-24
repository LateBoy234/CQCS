using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.ColorQualityControl.Adapter
{
    delegate string UpdateSpecimenColorIndex(SpecimenAdapter specimen);

    delegate string UpdateTestColorIndex(StandardSpecimenAdapter standard, TestSpecimenAdapter test);

    /// <summary>
    /// 简单的样品颜色指数类
    /// </summary>
    class SimpleSpecimenColorIndex : ISpecimenColorIndex
    {
        private UpdateSpecimenColorIndex _updateColorIndex;

        SimpleSpecimenColorIndex(string name, UpdateSpecimenColorIndex update)
        {
            Name = name;
            _updateColorIndex = update;
        }

        public string Name { get; }

        public string Value { get; private set; }

        public void Update(SpecimenAdapter specimen)
        {
            Value = _updateColorIndex(specimen);
        }
    }

    /// <summary>
    /// 简单的试样颜色指数类
    /// </summary>
    class SimpleTestColorIndex : ITestColorIndex
    {
        private UpdateTestColorIndex _updateColorIndex;

        SimpleTestColorIndex(string name, UpdateTestColorIndex update)
        {
            Name = name;
            _updateColorIndex = update;
        }

        public string Name { get; private set; }
        public string Value { get; private set; }
        public void Update(StandardSpecimenAdapter standard, TestSpecimenAdapter test)
        {
            Value = _updateColorIndex(standard, test);
        }
    }
}
