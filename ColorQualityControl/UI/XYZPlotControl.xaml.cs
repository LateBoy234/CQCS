using LiveCharts.Wpf;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LiveCharts;
using ThreeNH.ColorAlgorithm;
using LiveCharts.Configurations;
using System.Windows.Media;
using System.Globalization;
using LiveCharts.Defaults;

namespace ThreeNH.ColorQualityControl.UI
{
    public interface IXYZPlotPoint
    {
        double X { get; }
        double Y { get; }
        double Z { get; }
        string Name { get; }
        Color Stroke { get; }
    }


    /// <summary>
    /// CoordinateSystemControl.xaml 的交互逻辑
    /// </summary>
    public partial class XYZPlotControl : UserControl, INotifyPropertyChanged
    {
        public XYZPlotControl()
        {
            InitializeComponent();

            xyChart.DataContext = this;
            zChart.DataContext = this;

            grid.SizeChanged += Grid_SizeChanged;
        }

        

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected CartesianChart XyChart { get { return xyChart; } }
        protected CartesianChart ZChart { get { return zChart; } }
        protected Axis AxisX { get { return axisX; } }
        protected Axis AxisY { get { return axisY; } }
        protected Axis AxisZ { get { return axisZ; } }

        private double xStep = 0.5;
        public double XStep
        {
            get { return xStep; }
            set
            {
                if (xStep != value)
                {
                    xStep = value;
                    OnPropertyChanged("XStep");
                }
            }
        }
        private double yStep = 0.5;
        public double YStep
        {
            get { return yStep; }
            set
            {
                if (yStep != value)
                {
                    yStep = value;
                    OnPropertyChanged("YStep");
                }
            }
        }
        private double zStep = 0.5;
        public double ZStep
        {
            get { return zStep; }
            set
            {
                if (zStep != value)
                {
                    zStep = value;
                    OnPropertyChanged("ZStep");
                }
            }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged("Title");
            }
        }

        private string _xAsisTitle = "X";
        public string XAxisTitle
        {
            get { return _xAsisTitle; }
            set
            {
                _xAsisTitle = value;
                OnPropertyChanged("XAxisTitle");
            }
        }

        private string _yAxisTitle = "Y";
        public string YAxisTitle
        {
            get { return _yAxisTitle; }
            set
            {
                _yAxisTitle = value;
                OnPropertyChanged("YAxisTitle");
            }
        }

        private string _zAxisTitle = "Z";
        public string ZAxisTitle
        {
            get { return _zAxisTitle; }
            set
            {
                _zAxisTitle = value;
                OnPropertyChanged("ZAxisTitle");
            }
        }

        //protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        //{
        //    base.OnRenderSizeChanged(sizeInfo);
        //    if (grid == null)
        //        return;

        //    double totalWidth = grid.ActualWidth;
        //    double totalHeight = grid.ActualHeight;

        //    double zWidth = Math.Min(totalWidth / 4, 100.0);
        //    double width = totalWidth - zWidth;
        //    double height = grid.RowDefinitions[1].ActualHeight;
        //    double xyWidth = Math.Min(width, height);
        //    zWidth = Math.Min(xyWidth / 3.0, 100.0);

        //    double space = totalWidth - xyWidth - zWidth;
        //    grid.ColumnDefinitions[0].Width = new GridLength(xyWidth + space / 2, GridUnitType.Star);
        //    grid.ColumnDefinitions[1].Width = new GridLength(zWidth + space / 2, GridUnitType.Star);

        //    if (xyWidth > 0 && xyChart != null)
        //    {
        //        xyChart.Height = xyChart.Width = xyWidth;
        //        FormattedText formattedText = 
        //            new FormattedText("M", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
        //            new Typeface(axisX.FontFamily, axisX.FontStyle, axisX.FontWeight, axisX.FontStretch), 
        //            axisX.FontSize, Brushes.Black);

        //        double axisHeight = formattedText.Height * 2;
        //        zChart.Height = xyWidth - axisHeight;
        //        zChart.Margin = new Thickness(0, 0, 0, axisHeight);
        //    }
        //}

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double totalWidth = grid.ActualWidth;
            double totalHeight = grid.ActualHeight;

            double zWidth = 80.0;
            double width = totalWidth - zWidth;
            double height = grid.RowDefinitions[1].ActualHeight;
            double xyWidth = Math.Min(width, height);

            double space = totalWidth - xyWidth - zWidth;
            grid.ColumnDefinitions[0].Width = new GridLength(xyWidth + space / 2, GridUnitType.Star);
            grid.ColumnDefinitions[1].Width = new GridLength(zWidth + space / 2, GridUnitType.Star);

            if (xyWidth > 0 && xyChart != null)
            {
                xyChart.Height = xyChart.Width = xyWidth;
                FormattedText formattedText =
                    new FormattedText("M", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    new Typeface(axisX.FontFamily, axisX.FontStyle, axisX.FontWeight, axisX.FontStretch),
                    axisX.FontSize, Brushes.Black);
                    xyChart.Margin = new Thickness(0, 0, 0, 0);
                double axisHeight = formattedText.Height * 2;
                zChart.Height = xyWidth - axisHeight;
                zChart.Margin = new Thickness(0, 0, 0, axisHeight);
            }
        }

        public virtual void Clear()
        {
            foreach(var series in XyChart.Series)
            {
                if (series.Values != null)
                    series.Values.Clear();
            }
            foreach(var series in zChart.Series)
            {
                if (series.Values != null)
                    series.Values.Clear();
            }
        }

        public VisualElementsCollection VisualElements { get; set; } = new VisualElementsCollection();
    }
}
