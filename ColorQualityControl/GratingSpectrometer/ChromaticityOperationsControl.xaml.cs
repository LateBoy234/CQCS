using System;
using System.Windows;
using System.Windows.Controls;
using ThreeNH.Optical;
using ThreeNH.ColorAlgorithm;


namespace ThreeNH.GratingSpectrometer
{
    /// <summary>
    /// ChromaticityOperationsControl.xaml 的交互逻辑
    /// </summary>
    public partial class ChromaticityOperationsControl : UserControl
    {
        public ChromaticityOperationsControl()
        {
            InitializeComponent();
            
            foreach(Instrument.ColorSpace item in Enum.GetValues(typeof(Instrument.ColorSpace)))
            {
                var colorSpace = item.Uniform();
                colorSpaceComboBox.Items.Add(
                    new ComboBoxItem() { Content = colorSpace.ToString(), Tag = colorSpace });
            }
            ColorSpace = ColorSpace.CIE_LAB;

            foreach (Instrument.ColorDiffFormula item in Enum.GetValues(typeof(Instrument.ColorDiffFormula)))
            {
                var colorSpace = item.Uniform();
                colorDiffFormulaComboBox.Items.Add(
                    new ComboBoxItem() { Content = colorSpace.ToString(), Tag = colorSpace });
            }
            ColorDiffFormula = ColorDiffFormula.CIE1976;

            DataContext = this;
        }


        public bool Verify()
        {
            return true;
        }

        public static readonly DependencyProperty ChromaticityOperationsProperty =
            DependencyProperty.Register("ChromaticityOperations",
                typeof(ChromaticityOperations), typeof(ChromaticityOperationsControl),
                new PropertyMetadata(null, OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ChromaticityOperationsControl;
            if (control != null && e.Property == ChromaticityOperationsProperty)
            {
                control.ColorSpace = control.ChromaticityOperations.ColorSpace;
                control.ColorDiffFormula = control.ChromaticityOperations.ColorDiffFormula;
            }
        }

        public ChromaticityOperations ChromaticityOperations
        {
            get { return (ChromaticityOperations)GetValue(ChromaticityOperationsProperty); }
            set { SetValue(ChromaticityOperationsProperty, value); }
        }

        ColorSpace ColorSpace
        {
            get
            {
                return (ColorSpace)(colorSpaceComboBox.SelectedItem as ComboBoxItem).Tag;
            }
            set
            {
                foreach (ComboBoxItem item in colorSpaceComboBox.Items)
                {
                    if ((ColorSpace)item.Tag == value)
                    {
                        colorSpaceComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        ColorDiffFormula ColorDiffFormula
        {
            get
            {
                return (ColorDiffFormula)(colorDiffFormulaComboBox.SelectedItem as ComboBoxItem).Tag;
            }
            set
            {
                foreach (ComboBoxItem item in colorDiffFormulaComboBox.Items)
                {
                    if ((ColorDiffFormula)item.Tag == value)
                    {
                        colorDiffFormulaComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void OnColorSpaceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChromaticityOperations != null)
            {
                ChromaticityOperations.ColorSpace = ColorSpace;
            }
        }

        private void OnColorDiffFormulaSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChromaticityOperations != null)
            {
                ChromaticityOperations.ColorDiffFormula = ColorDiffFormula;
            }
        }
    }
}
