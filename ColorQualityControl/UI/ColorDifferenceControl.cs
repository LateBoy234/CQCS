using System.Windows;
using System.Windows.Documents;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorQualityControl.Reports;

namespace ThreeNH.ColorQualityControl.UI
{
    public partial class ColorDifferenceControl
    {
        ChromaticityAnalyzeReport _report = new ChromaticityAnalyzeReport();
        SpecimenWrap _standard;
        SpecimenWrap _test;

        public ColorDifferenceControl()
        {
            InitializeComponent();
        }

        MainWindow MainWindow
        {
            get { return (Application.Current.MainWindow as MainWindow); }
        }

        public FlowDocument Document
        {
            get { return _report.GetReport(); }
        }

        internal SpecimenWrap Standard
        {
            get { return _standard; }
            set
            {
                _standard = value;
                _report.Standard = value == null ? null : value.Adapter as StandardSpecimenAdapter;
                if (IsInitialized)
                {
                    richTextBox.Document = _report.GetReport();
                }
            }
        }

        internal SpecimenWrap Test
        {
            get { return _test; }
            set
            {
                _test = value;
                _report.Test = value == null ? null : value.Adapter as TestSpecimenAdapter;
                if (IsInitialized)
                {
                    richTextBox.Document = _report.GetReport();
                }
            }
        }

        internal void Refresh()
        {
            _report.Standard = _standard == null ? null : _standard.Adapter as StandardSpecimenAdapter;
            _report.Test = _test == null ? null : _test.Adapter as TestSpecimenAdapter;

            if (IsInitialized)
            {
                richTextBox.Document = _report.GetReport();
            }
        }
    }
}
