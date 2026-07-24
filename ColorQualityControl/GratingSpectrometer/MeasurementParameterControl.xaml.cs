using System;
using System.Windows;
using System.Windows.Controls;
using ThreeNH.Optical;
using ThreeNH.Helper;

namespace ThreeNH.GratingSpectrometer
{
    /// <summary>
    /// MeasurementParameterControl.xaml 的交互逻辑
    /// </summary>
    public partial class MeasurementParameterControl : UserControl
    {

        public MeasurementParameterControl()
        {
            InitializeComponent();

            var filter = new Predicate<Enum>(e => (StandardIlluminant)e != StandardIlluminant.Unknown);
            PrimaryIlluminant.ItemsSource = EnumHelper.GetAllValuesAndDescriptions(typeof(StandardIlluminant), filter);//IlluminantExtension.IlluminantNames;
            PrimaryIlluminant.DisplayMemberPath = "Description";
            PrimaryIlluminant.SelectedValuePath = "Value";
            PrimaryIlluminant.SelectedValue = StandardIlluminant.D65;

            SecondaryIlluminant.ItemsSource = EnumHelper.GetAllValuesAndDescriptions(typeof(StandardIlluminant), filter);//IlluminantExtension.IlluminantNames;
            SecondaryIlluminant.DisplayMemberPath = "Description";
            SecondaryIlluminant.SelectedValuePath = "Value";
            SecondaryIlluminant.SelectedValue = StandardIlluminant.C;

            TertiaryIlluminant.ItemsSource = EnumHelper.GetAllValuesAndDescriptions(typeof(StandardIlluminant), filter);//IlluminantExtension.IlluminantNames;
            TertiaryIlluminant.DisplayMemberPath = "Description";
            TertiaryIlluminant.SelectedValuePath = "Value";
            TertiaryIlluminant.SelectedValue = StandardIlluminant.F2;
        }

        public bool Verify()
        {
            return true;
        }

        public MeasurementParameter MeasurementParameter
        {
            get
            {
                MeasurementParameter parameter = new MeasurementParameter();
                parameter.PrimaryIlluminant = (StandardIlluminant)PrimaryIlluminant.SelectedValue;
                parameter.SecondaryIlluminant = (StandardIlluminant)SecondaryIlluminant.SelectedValue;
                parameter.TertiaryIlluminant = (StandardIlluminant)TertiaryIlluminant.SelectedValue;

                if (CIE1931.IsChecked == true)
                    parameter.Observer = StandardObserver.CIE1931;
                else
                    parameter.Observer = StandardObserver.CIE1964;

                if (SCI.IsChecked == true)
                    parameter.SpecularComponent = SpecularComponent.SCI;
                else if (SCE.IsChecked == true)
                    parameter.SpecularComponent = SpecularComponent.SCE;
                else
                    parameter.SpecularComponent = SpecularComponent.SCIAndSCE;

                if (Phi4.IsChecked == true)
                    parameter.MeasurementCaliber = MeasurementCaliber.Phi4;
                else if (Phi8.IsChecked == true)
                    parameter.MeasurementCaliber = MeasurementCaliber.Phi8;
                else if (Phi8Plus.IsChecked == true)
                    parameter.MeasurementCaliber = MeasurementCaliber.Phi8Plus;
                else
                    parameter.MeasurementCaliber = MeasurementCaliber.Unknown;

                parameter.UV = (UVFlag)(int)UV.Value;

                return parameter;
            }
            set
            {
                if (value == null)
                {
                    return;
                }
                PrimaryIlluminant.SelectedValue = value.PrimaryIlluminant;
                SecondaryIlluminant.SelectedValue = value.SecondaryIlluminant;
                TertiaryIlluminant.SelectedValue = value.TertiaryIlluminant;

                if (value.Observer == StandardObserver.CIE1931)
                {
                    CIE1931.IsChecked = true;
                }
                else
                {
                    CIE1964.IsChecked = true;
                }

                switch(value.SpecularComponent)
                {
                    case SpecularComponent.SCI:
                        SCI.IsChecked = true;
                        break;
                    case SpecularComponent.SCE:
                        SCE.IsChecked = true;
                        break;
                    case SpecularComponent.SCIAndSCE:
                        SCIAndSCE.IsChecked = true;
                        break;
                }

                switch(value.MeasurementCaliber)
                {
                    case MeasurementCaliber.Phi4:
                        Phi4.IsChecked = true;
                        break;
                    case MeasurementCaliber.Phi8:
                        Phi8.IsChecked = true;
                        break;
                    case MeasurementCaliber.Phi8Plus:
                        Phi8Plus.IsChecked = true;
                        break;
                }

                UV.Value = (int)value.UV;
            }
        }
    }
}
