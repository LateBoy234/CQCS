using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.UI
{
    enum LayoutType
    {
        Horizontal,
        Vertical,
        Wrap
    }

    enum DeltaPlotType
    {
        DeltaLab,
        DeltaLCH,
        DeltaLuv,
        DeltaHunterLab,
        DeltaYxy,
        Count
    }

    class DeltaPlotAgentControl : UserControl
    {
        private List<SpecimenWrap> _tests = new List<SpecimenWrap>();
        private DeltaPlotControl[] _plots = new DeltaPlotControl[(int)DeltaPlotType.Count];

        private static Func<TestSpecimenAdapter, IDeltaPlotPoint>[] _pointConverter = new Func<TestSpecimenAdapter, IDeltaPlotPoint>[]
        {
                test => new DeltaLabPoint(test),
                test => new DeltaLChPoint(test),
                test => new DeltaLuvPoint(test),
                test => new DeltaHunterLabPoint(test),
                test => new DeltaYxyPoint(test)
        };
        private static Func<DeltaPlotControl>[] _plotCreator = new Func<DeltaPlotControl>[]
        {
            () =>  new DeltaPlotControl(ChromaticityField.da.Name(), ChromaticityField.db.Name(), ChromaticityField.dL.Name()),
            () =>  new DeltaPlotControl(ChromaticityField.dC.Name(), ChromaticityField.dH.Name(), ChromaticityField.dL.Name()),
            () =>  new DeltaPlotControl(ChromaticityField.du.Name(), ChromaticityField.dv.Name(), ChromaticityField.dL.Name()),
            () =>  new DeltaPlotControl(ChromaticityField.dHa.Name(), ChromaticityField.dHb.Name(), ChromaticityField.dHL.Name()),
            () =>  new DeltaPlotControl(ChromaticityField.dx.Name(), ChromaticityField.dy.Name(), ChromaticityField.dY.Name())
        };

        public DeltaPlotAgentControl()
        {
            for (int i = 0; i < _plots.Length; i++)
            {
                _plots[i] = _plotCreator[i]();
                _plots[i].BorderBrush = Brushes.DarkCyan;
                _plots[i].BorderThickness = new Thickness(2);
                _plots[i].Padding = new Thickness(6);
                _plots[i].Margin = new Thickness(3);
                _plots[i].Visibility = Visibility.Collapsed;
            }

            Content = new Grid();
            LayoutType = LayoutType.Horizontal;
            Background = SystemColors.WindowBrush ;
        }

        public DeltaPlotControl GetDeltaPlot(DeltaPlotType type)
        {
            return _plots[(int)type];
        }

        internal void Add(SpecimenWrap test)
        {
            _tests.Add(test);
            for (int i = 0; i < _plots.Length; i++)
            {
                if (_plots[i].IsVisible)
                    _plots[i].Add(_pointConverter[i](test.Adapter as TestSpecimenAdapter));
            }
        }

        internal void Remove(SpecimenWrap test)
        {
            int index = _tests.IndexOf(test);
            if (index != -1)
            {
                foreach(var plot in _plots)
                {
                    if (plot.IsVisible)
                    {
                        plot.RemoveAt(index);
                    }
                }
                _tests.RemoveAt(index);
            }
        }

        internal void Refresh()
        {
            for (int i = 0; i < _plots.Length; i++)
            {
                var plot = _plots[i];
                if (!plot.IsVisible)
                    continue;
                var converter = _pointConverter[i];
                for (int index = 0; index < _tests.Count; index++)
                    plot.SetPoint(index, converter(_tests[index].Adapter as TestSpecimenAdapter));
                plot.UpdateChart();
            }
        }

        internal void SelectSinglae(TestSpecimen test)
        {
            foreach(var plot in _plots)
            {
                if (!plot.IsVisible)
                    continue;
                plot.SelectSingle(pt => (pt as DeltaPoint).Test == test);
            }
        }

        internal void UnselectAll()
        {
            foreach (var plot in _plots)
            {
                if (!plot.IsVisible)
                    continue;
                plot.UnselectAll();
            }
        }

        internal void Clear()
        {
            foreach(var plot in _plots)
            {
                if (plot.IsVisible)
                    plot.Clear();
            }
            _tests.Clear();
        }

        internal void Show(DeltaPlotType deltaType)
        {
            var plot = _plots[(int)deltaType];

            var converter = _pointConverter[(int)deltaType];
            foreach(var test in _tests)
            {
                plot.Add(converter(test.Adapter as TestSpecimenAdapter));
            }
            plot.Visibility = Visibility;

            var grid = Content as Grid;
            if (LayoutType == LayoutType.Horizontal)
            {
                grid.ColumnDefinitions[(int)deltaType].Width = new GridLength(1, GridUnitType.Star);
            }
            else if (LayoutType == LayoutType.Vertical)
            {
                grid.RowDefinitions[(int)deltaType].Height = new GridLength(1, GridUnitType.Star);
            }
        }

        internal void Hide(DeltaPlotType deltaType)
        {
            if (_plots[(int)deltaType] != null)
            {
                _plots[(int)deltaType].Visibility = Visibility.Collapsed;
                _plots[(int)deltaType].Clear();
                var grid = Content as Grid;
                if (LayoutType == LayoutType.Horizontal)
                {
                    grid.ColumnDefinitions[(int)deltaType].Width = GridLength.Auto;
                }
                else if (LayoutType == LayoutType.Vertical)
                {
                    grid.RowDefinitions[(int)deltaType].Height = GridLength.Auto;
                }
            }
        }


        internal bool IsShow(DeltaPlotType deltaType)
        {
            return _plots[(int)deltaType] != null && _plots[(int)deltaType].IsVisible;
        }

        internal DeltaPlotType[] VisibleDeltaPlots
        {
            get
            {
                List<DeltaPlotType> list = new List<DeltaPlotType>();
                for (int i = 0; i < (int)DeltaPlotType.Count; i++)
                {
                    if (IsShow((DeltaPlotType) i))
                    {
                        list.Add((DeltaPlotType)i);
                    }
                }
                return list.ToArray();
            }
        }

        internal static DeltaPlotControl CreateDeltaPlotControl(DeltaPlotType deltaType)
        {
            return _plotCreator[(int)deltaType]();
        }

        internal static Func<TestSpecimenAdapter, IDeltaPlotPoint> GetPointConverter(DeltaPlotType deltaType)
        {
            return _pointConverter[(int)deltaType];
        }

        /// <summary>
        /// 设置布局类型
        /// </summary>
        /// <param name="layoutType">0，表示堆叠，1表示水平并排，2表示上下并排，3表示格子平铺</param>
        void SetLayoutType(LayoutType layoutType)
        {

            Grid grid = Content as Grid;
            if (grid.Children.Count == 1 &&  grid.Children[0] is WrapPanel)
            {
                WrapPanel wrap = grid.Children[0] as WrapPanel;
                wrap.Children.Clear();
            }
            grid.Children.Clear();
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();

            if (layoutType == LayoutType.Horizontal)
            {
                grid.RowDefinitions.Add(new RowDefinition());
                grid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);

                for (int i = 0, col = 0; i < _plots.Length; i++)
                {
                    var plot = _plots[i];
                    ColumnDefinition coldef = new ColumnDefinition();
                    grid.ColumnDefinitions.Add(coldef);
                    coldef.Width = plot.Visibility == Visibility.Visible ?
                        new GridLength(1, GridUnitType.Star) : GridLength.Auto;

                    grid.Children.Add(plot);
                    Grid.SetColumn(plot, col);
                    Grid.SetRow(plot, 0);

                    col += 1;
                }
            }
            else if (layoutType == LayoutType.Vertical)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);

                for (int i = 0; i < _plots.Length; i++)
                {
                    var plot = _plots[i];
                    RowDefinition rowdef = new RowDefinition();
                    grid.RowDefinitions.Add(rowdef);
                    rowdef.Height = plot.Visibility == Visibility.Visible ?
                        new GridLength(1, GridUnitType.Star) : GridLength.Auto;

                    grid.Children.Add(plot);
                    Grid.SetColumn(plot, 0);
                    Grid.SetRow(plot, i);
                }
            }
            else if (layoutType == LayoutType.Wrap)
            {
                grid.RowDefinitions.Add(new RowDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);

                WrapPanel wrap = new WrapPanel();
                wrap.VerticalAlignment = VerticalAlignment.Stretch;
                wrap.HorizontalAlignment = HorizontalAlignment.Stretch;
                //wrap.Background = Brushes.LightBlue;
                grid.Children.Add(wrap);
                Grid.SetRow(wrap, 0);
                Grid.SetColumn(wrap, 0);

                foreach(var plot in _plots)
                {
                    wrap.Children.Add(plot);
                }
            }
        }

        //protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        //{
        //    base.OnRenderSizeChanged(sizeInfo);
        //    InvalidateVisual();
        //}

        LayoutType _layoutType = LayoutType.Horizontal;
        internal LayoutType LayoutType
        {
            get { return _layoutType;  }
            set
            {
                _layoutType = value;
                SetLayoutType(_layoutType);
            }
        }

    }
}
