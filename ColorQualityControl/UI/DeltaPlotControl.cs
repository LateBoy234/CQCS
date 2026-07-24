using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LiveCharts.Defaults;
using LiveCharts;
using LiveCharts.Wpf;
using System.Windows.Media;
using LiveCharts.Configurations;
using System.Windows;
using System.Windows.Forms;

namespace ThreeNH.ColorQualityControl.UI
{
    interface IDeltaPlotPoint : IObservableChartPoint, IEquatable<IDeltaPlotPoint>
    {
        string Name { get; }
        JudgementLevel Judgement { get; }
        bool IsSelected { get; set; }
        double X { get; }
        double Y { get; }
        double Z { get; }
        Brush StrokeLilin { get; }
    }

    class CommonPlotPoint : IDeltaPlotPoint
    {
        public event Action PointChanged;
        public bool Equals(IDeltaPlotPoint other)
        {
            return Name == other?.Name;
        }

        public string Name { get; set; }
        public JudgementLevel Judgement { get; set; }
        public bool IsSelected { get; set; }
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public Brush StrokeLilin { get; }

        public CommonPlotPoint(string name, double x, double y, double z, object color = null)
        {
            X = x;
            Y = y;
            Z = z;
            Name = name;

            Judgement = JudgementLevel.Pass;
            StrokeLilin = color == null ? Brushes.Gray : (Brush)color;
        }
    }


    class DeltaPlotControl : XYZPlotControl
    {
        ChartValues<IDeltaPlotPoint> values =
            new ChartValues<IDeltaPlotPoint>();

        public DeltaPlotControl()
            : this("\u0394X", "\u0394Y", "\u0394Z")
        {

        }

        public DeltaPlotControl(string xTitle, string yTitle, string zTitle)
        {
            XYMinRange = 1.0;
            ZMinRange = 1.0;

            JudgementBrushes = new Dictionary<JudgementLevel, Brush>();
            JudgementBrushes.Add(JudgementLevel.Invalid, Brushes.Gray);
            JudgementBrushes.Add(JudgementLevel.Pass, Brushes.Gray);
            JudgementBrushes.Add(JudgementLevel.Warning, Brushes.Yellow);
            JudgementBrushes.Add(JudgementLevel.NotGood, Brushes.Red);
            SelectedBrush = SystemColors.HighlightBrush;


            AxisX.MinValue = -1.0;
            AxisX.MaxValue = 1.0;
            XAxisTitle = xTitle;

            AxisY.MinValue = -1.0;
            AxisY.MaxValue = 1.0;
            YAxisTitle = yTitle;

            AxisZ.MinValue = -1.0;
            AxisZ.MaxValue = 1.0;
            ZAxisTitle = zTitle;

            ScatterSeries series = new ScatterSeries();
            XyChart.Series.Add(series);
            series.Fill = Brushes.Gray;
            series.Values = values;
            series.Title = "";
            series.Configuration = Mappers.Xy<IDeltaPlotPoint>()
                .X(v => v.X)
                .Y(v => v.Y)
                .Fill(v => v.IsSelected ? SelectedBrush : JudgementBrushes[v.Judgement]);
            series.LabelPoint = c => string.Format("{0}\n{1}: {2:F3}\n{3}: {4:F3}",
                values[c.Key].Name, XAxisTitle, c.X, YAxisTitle, c.Y);

            series.MinPointShapeDiameter = 4;
            series.MaxPointShapeDiameter = 8;

            series = new ScatterSeries();
            ZChart.Series.Add(series);
            series.Values = values;
            series.Title = "";
            series.Fill = Brushes.Gray;
            series.Configuration = Mappers.Xy<IDeltaPlotPoint>()
                .X(v => 0.0)
                .Y(v => v.Z)
                .Fill(v => v.IsSelected ? SelectedBrush : JudgementBrushes[v.Judgement]);
            series.LabelPoint = c => string.Format("{0}\n{1}: {2:F3}", values[c.Key].Name, ZAxisTitle, c.Y);
            series.MinPointShapeDiameter = 4;
            series.MaxPointShapeDiameter = 8;
        }

        // XY的最小范围
        public double XYMinRange
        {
            get; set;
        }
        // Z值的最小范围
        public double ZMinRange
        {
            get; set;
        }

        /// <summary>
        /// 不同判定结果的点的颜色
        /// </summary>
        public Dictionary<JudgementLevel, Brush> JudgementBrushes
        {
            get; set;
        }
        /// <summary>
        /// 选中项的颜色
        /// </summary>
        public Brush SelectedBrush
        {
            get; set;
        }

        /// <summary>
        /// 设置或获取选中项（如果有多个选中项，只返回第一个选中项，而设置则会清除所有其他选中项）
        /// </summary>
        public IDeltaPlotPoint SelectedItem
        {
            get
            {
                int index = IndexOf(pt => pt.IsSelected);
                if (index < 0)
                    return null;
                return values[index];
            }
            set
            {
                int index = IndexOf(pt => pt.IsSelected);
                if (index >= 0)
                    values[index].IsSelected = false;

                if (value != null)
                {
                    index = IndexOf(value);
                    if (index >= 0)
                        values[index].IsSelected = true;
                }
            }
        }

        /// <summary>
        /// 当前选中的个数
        /// </summary>
        public int SelectedCount
        {
            get
            {
                return values.Count(item => item.IsSelected);
            }
        }
        /// <summary>
        /// 获取当前选中的所有项
        /// </summary>
        public IDeltaPlotPoint[] SelectedItems
        {
            get
            {
                return (from item in values where item.IsSelected select item).ToArray();
            }
        }

        public void SelectSingle(Func<IDeltaPlotPoint, bool> predicate)
        {
            UnselectAll(false);
            Select(predicate, true);
        }

        /// <summary>
        /// 选中令predicate返回真的所有项
        /// </summary>
        /// <param name="predicate"></param>
        public void Select(Func<IDeltaPlotPoint, bool> predicate, bool update = true)
        {
            foreach (var item in values)
            {
                if (predicate(item))
                    item.IsSelected = true;
            }
            if (update)
                UpdateChart();
        }

        /// <summary>
        /// 取消选中令predicate返回真的所有有项
        /// </summary>
        /// <param name="predicate"></param>
        public void Unselect(Func<IDeltaPlotPoint, bool> predicate, bool update = true)
        {
            foreach (var item in values)
            {
                if (predicate(item))
                    item.IsSelected = false;
            }
            if (update)
                UpdateChart();
        }

        /// <summary>
        /// 取消所有选中项
        /// </summary>
        public void UnselectAll(bool update = true)
        {
            foreach (var item in values)
            {
                if (item.IsSelected)
                    item.IsSelected = false;
            }
            if (update)
                UpdateChart();
        }

        public void UpdateChart()
        {
            AdjustAxisRange();
            xyChart.Update();
            zChart.Update();
        }

        /// <summary>
        /// 向图中添加一个点
        /// </summary>
        /// <param name="point"></param>
        public void Add(IDeltaPlotPoint point)
        {
            if (double.IsNaN(point.X) || double.IsNaN(point.Y) || double.IsNaN(point.Z))
                return;
            if (point == null)
                return;

            values.Add(point);

            // 根据加入的点的值自动调整坐标系的范围
            AdjustAxisRange();
        }

        void RoundAxisRange(ref double range, out double step)
        {
            step = range / 4 * 2;
            step = Math.Ceiling(step) * 0.5;
            if (range <= step * 2)
                range = step * 2.0;
            else if (range <= step * 3)
                range = step * 3.0;
            else
                range = step * 4.0;
        }

        private void AdjustAxisRange()
        {
            if (values.Count == 0)
                return;

            double maxX = values.Max(v => Math.Abs(v.X));
            double maxY = values.Max(v => Math.Abs(v.Y));

            double range = Math.Max(XYMinRange, Math.Max(maxX, maxY));
            double step;
            RoundAxisRange(ref range, out step);
            AxisX.MinValue = -range;
            AxisX.MaxValue = range;
            AxisX.Separator.Step = step;
            AxisY.MinValue = -range;
            AxisY.MaxValue = range;
            AxisY.Separator.Step = step;

            range = values.Max(v => Math.Abs(v.Z));
            range = Math.Max(ZMinRange, range);
            RoundAxisRange(ref range, out step);
            AxisZ.MinValue = -range;
            AxisZ.MaxValue = range;
            AxisZ.Separator.Step = step;
        }

        public int IndexOf(Func<IDeltaPlotPoint, bool> predicate)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (predicate(values[i]))
                    return i;
            }
            return -1;
        }
        public int IndexOf(IDeltaPlotPoint point)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Equals(point))
                    return i;
            }
            return -1;
        }

        public void RemoveAt(int index)
        {
            if(values!=null&&values.Count>index)
            values.RemoveAt(index);
        }

        /// <summary>
        /// 移除一个点
        /// </summary>    
        /// <param name="point"></param>
        public void Remove(IDeltaPlotPoint point)
        {
            if (point == null)
                return;

            RemoveAt(IndexOf(point));
            AdjustAxisRange();
        }

        public override void Clear()
        {
            base.Clear();
        }

        public void SetPoint(int index, IDeltaPlotPoint point)
        {
            if (values != null && values.Count > index)
                values[index] = point;
        }
    }
}
