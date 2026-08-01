using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ThreeNH.CommunicationBase;
using ThreeNH.SQCX.Infrastructure.Instruments.YRColorimeter;

namespace ThreeNH.ColorQualityControl.YRColorimeter
{
    /// <summary>
    /// YRWhiteBoardSettingDlg.xaml 的交互逻辑
    /// </summary>
    public partial class YRWhiteBoardSettingDlg : Window, INotifyPropertyChanged
    {
        private Colorimeter colorimeter;

        public YRWhiteBoardSettingDlg(Colorimeter colorimeter)
        {
            this.colorimeter = colorimeter;
            DataContext = this;
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public double _x;
        public double _y;
        public double _z;

        public double X
        {
            get { return _x; }
            set
            {
                if (_x != value)
                {
                    _x = value;
                    NotifyPropertyChanged("X");
                }
            }
        }
        public double Y
        {
            get { return _y; }
            set
            {
                if (_y != value)
                {
                    _y = value;
                    NotifyPropertyChanged("Y");
                }
            }
        }
        public double Z
        {
            get { return _z; }
            set
            {
                if (_z != value)
                {
                    _z = value;
                    NotifyPropertyChanged("Z");
                }
            }
        }

        private void GetBtn_Click(object sender, RoutedEventArgs e)
        {
            float x, y, z;

            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                colorimeter.GetWhiteBoardXYZ(out x, out y, out z);
                X = x;
                Y = y;
                Z = z;
            }
            catch (CommunicationException ex)
            {
                MessageBox.Show(S._(ex.ErrorCode.ToString()));
            }
            finally
            {
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }

        private void SetBtn_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                colorimeter.SetWhiteBoardXYZ((float)X, (float)Y, (float)Z);
                MessageBox.Show("操作成功返回");
            }
            catch (CommunicationException ex)
            {
                MessageBox.Show(S._(ex.ErrorCode.ToString()));
            }
            finally
            {
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }
    }
}
