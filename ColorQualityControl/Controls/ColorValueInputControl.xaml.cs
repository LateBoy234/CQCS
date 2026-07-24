using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ThreeNH.Optical;
using ThreeNH.ColorAlgorithm;
using System.Windows.Input;

namespace ThreeNH.Controls
{
    /// <summary>
    /// ColorDataInputControl.xaml 的交互逻辑
    /// </summary>
    public partial class ColorValueInputControl : UserControl, INotifyPropertyChanged
    {
        public ColorValueInputControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public static DependencyProperty ColorSpaceProperty =
            DependencyProperty.Register("ColorSpace", typeof(ColorSpace), 
                typeof(ColorValueInputControl), new PropertyMetadata(ColorSpace.CIE_LAB, OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ColorValueInputControl;
            if (e.Property == ColorSpaceProperty)
            {
                control.OnColorSpaceChanged((ColorSpace)e.NewValue);
            }
        }

        private void OnColorValueChanged(object newValue)
        {
            if (newValue is CIELAB)
            {
                CIELAB lab = newValue as CIELAB;
                if (ColorSpace == ColorSpace.CIE_LAB)
                {
                    
                    _value1 = lab.L;
                    _value2 = lab.a;
                    _value3 = lab.b;
                }
                else if (ColorSpace == ColorSpace.CIE_LCH)
                {
                    _value1 = lab.L;
                    _value2 = lab.C;
                    _value3 = lab.h;
                }
                else
                {
                    ColorSpace = ColorSpace.CIE_LAB;
                    _value1 = lab.L;
                    _value2 = lab.a;
                    _value3 = lab.b;
                }
            }
            else if (newValue is CIEXYZ)
            {
                CIEXYZ xyz = newValue as CIEXYZ;
                if (ColorSpace == ColorSpace.CIE_XYZ)
                {
                    _value1 = xyz.X;
                    _value2 = xyz.Y;
                    _value3 = xyz.Z;
                }
                else if (ColorSpace == ColorSpace.CIE_Yxy)
                {
                    _value1 = xyz.Y;
                    _value2 = xyz.x;
                    _value3 = xyz.y;
                }
                else
                {
                    ColorSpace = ColorSpace.CIE_XYZ;
                    _value1 = xyz.X;
                    _value2 = xyz.Y;
                    _value3 = xyz.Z;
                }
            }
            else if (newValue is CIELUV)
            {
                CIELUV luv = newValue as CIELUV;
                if (ColorSpace != ColorSpace.CIE_LUV)
                {
                    ColorSpace = ColorSpace.CIE_LUV;
                }
                _value1 = luv.L;
                _value2 = luv.u;
                _value3 = luv.v;
            }
            else if (newValue is CIERGB)
            {
                CIERGB rgb = newValue as CIERGB;
                if (ColorSpace != ColorSpace.CIE_RGB)
                {
                    ColorSpace = ColorSpace.CIE_RGB;
                }
                _value1 = rgb.R;
                _value2 = rgb.G;
                _value3 = rgb.B;
            }
            else if (newValue is HunterLab)
            {
                HunterLab lab = newValue as HunterLab;
                if (ColorSpace == ColorSpace.CIE_LAB)
                {
                    ColorSpace = ColorSpace.HUNTER_LAB;
                }
                _value1 = lab.L;
                _value2 = lab.a;
                _value3 = lab.b;
            }

            NotifyPropertyChanged(nameof(Value1));
            NotifyPropertyChanged(nameof(Value2));
            NotifyPropertyChanged(nameof(Value3));
        }

        private void OnColorSpaceChanged(ColorSpace newValue)
        {
            switch(newValue)
            {
                case ColorSpace.CIE_LAB:
                    Label1.Text = "L*:";
                    Label2.Text = "a*:";
                    Label3.Text = "b*:";
                    break;
                case ColorSpace.CIE_LCH:
                    Label1.Text = "L*:";
                    Label2.Text = "C*:";
                    Label3.Text = "h\u00B0:";
                    break;
                case ColorSpace.CIE_XYZ:
                    Label1.Text = "X:";
                    Label2.Text = "Y:";
                    Label3.Text = "Z:";
                    break;
                case ColorSpace.CIE_Yxy:
                    Label1.Text = "Y:";
                    Label2.Text = "x:";
                    Label3.Text = "y:";
                    break;
                case ColorSpace.CIE_LUV:
                    Label1.Text = "L*:";
                    Label2.Text = "u*:";
                    Label3.Text = "v*:";
                    break;
                case ColorSpace.CIE_RGB:
                    Label1.Text = "R:";
                    Label2.Text = "G:";
                    Label3.Text = "B:";
                    break;
                case ColorSpace.HUNTER_LAB:
                    Label1.Text = "L:";
                    Label2.Text = "a:";
                    Label3.Text = "b:";
                    break;
            }
        }

        public ColorSpace ColorSpace
        {
            get { return (ColorSpace)GetValue(ColorSpaceProperty); }
            set { SetValue(ColorSpaceProperty, value); }
        }

        //public static DependencyProperty ColorValueProperty =
        //    DependencyProperty.Register("ColorValue", typeof(object),
        //        typeof(ColorValueInputControl), new PropertyMetadata(OnPropertyChanged));


        /// <summary>
        /// 顏色值,可以是Lab
        /// </summary>
        public object ColorValue
        {
            get
            {

                switch (ColorSpace)
                {
                    case ColorSpace.CIE_LAB:
                        return new CIELAB(_value1, _value2, _value3);
                    case ColorSpace.CIE_LCH:
                        return new CIELCh(_value1, _value2, _value3).ToLab();
                    case ColorSpace.CIE_XYZ:
                        return new CIEXYZ(_value1, _value2, _value3);
                    case ColorSpace.CIE_Yxy:
                        return new CIEYxy(_value1, _value2, _value3).XYZ;
                    case ColorSpace.CIE_LUV:
                        return new CIELUV(_value1, _value2, _value3);
                    case ColorSpace.CIE_RGB:
                        return new CIERGB(_value1, _value2, _value3);
                    case ColorSpace.HUNTER_LAB:
                        return new HunterLab(_value1, _value2, _value3);
                }
                return null;
            }
            set
            {
                OnColorValueChanged(value);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        double _value1, _value2, _value3;

        public double Value1
        {
            get { return _value1; }
            set
            {
                if (_value1 != value)
                {
                    _value1 = value;
                    NotifyPropertyChanged("Value1");
                }
            }
        }


        private void KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // 过滤按键
            //if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            //{
            //    e.Handled = false;
            //}
            //else if (((e.Key >= Key.D0 && e.Key <= Key.D9)))
            //{
            //    e.Handled = false;
            //}
            //else if (e.Key == Key.OemPeriod || e.Key == Key.Decimal||e.Key == Key.Subtract)
            //{
            //    e.Handled = false;
            //}
            //else
            //{
            //    e.Handled = true;
            //}
        }




        public double Value2
        {
            get { return _value2; }
            set
            {
                if (_value2 != value)
                {
                    _value2 = value;
                    NotifyPropertyChanged("Value2");
                }
            }
        }

        public double Value3
        {
            get { return _value3; }
            set
            {
                if (_value3 != value)
                {
                    _value3 = value;
                    NotifyPropertyChanged("Value3");
                }
            }
        }

    }
}
