using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ThreeNH.Helper
{
    public class GridHelper
    {
        public static readonly DependencyProperty ShowBorderProperty=
            DependencyProperty.RegisterAttached("ShowBorder", typeof(bool), typeof(GridHelper),
        new PropertyMetadata(OnShowBorderChanged));

        public static readonly DependencyProperty GridLineThicknessProperty =
            DependencyProperty.RegisterAttached("GridLineThickness", typeof(double), typeof(GridHelper),
            new PropertyMetadata(1.0, OnGridLineThicknessChanged));

        public static readonly DependencyProperty GridLineBrushProperty =
            DependencyProperty.RegisterAttached("GridLineBrush", typeof(Brush), typeof(GridHelper),
            new PropertyMetadata(OnGridLineBrushChanged));

        public static readonly DependencyProperty GridCellPaddingProperty =
            DependencyProperty.RegisterAttached("GridCellPadding", typeof(Thickness), typeof(GridHelper),
                new PropertyMetadata(new Thickness(3), OnGridCellPaddingChanged));

        #region ShowBorder
        public static bool GetShowBorder(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowBorderProperty);
        }
        public static void SetShowBorder(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowBorderProperty, value);
        }
        
        private static void OnShowBorderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as Grid;
            if ((bool)e.OldValue)
            {
                grid.Loaded -= (s, arg) => { };
            }
            if ((bool)e.NewValue)
            {
                grid.Loaded += new RoutedEventHandler(GridLoaded);                
            }
        }
        #endregion

        #region GridLineThickness
        public static double GetGridLineThickness(DependencyObject obj)
        {
            return (double)obj.GetValue(GridLineThicknessProperty);
        }
        public static void SetGridLineThickness(DependencyObject obj, double value)
        {
            obj.SetValue(GridLineThicknessProperty, value);
        }
        private static void OnGridLineThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            
        }
        #endregion

        #region GridLineBrush
        public static Brush GetGridLineBrush(DependencyObject obj)
        {
            Brush brush = (Brush)obj.GetValue(GridLineBrushProperty);
            return brush == null ? Brushes.LightGray : brush;
        }
        public static void SetGridLineBrush(DependencyObject obj, Brush value)
        {
            obj.SetValue(GridLineBrushProperty, value);
        }
        private static void OnGridLineBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }
        #endregion

        #region GridCellPadding
        public static Thickness GetGridCellPadding(DependencyObject obj)
        {
            return (Thickness)obj.GetValue(GridCellPaddingProperty);
        }
        public static void SetGridCellPadding(DependencyObject obj, Thickness value)
        {
            obj.SetValue(GridCellPaddingProperty, value);
        }
        private static void OnGridCellPaddingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        #endregion


        private static void GridLoaded(object sender, RoutedEventArgs e)
        {
            Grid grid = sender as Grid;
            var row_count = grid.RowDefinitions.Count;
            var column_count = grid.ColumnDefinitions.Count;

            #region 第一版：单元格合并后，无法正确显示GridLine
            //var rows = grid.RowDefinitions.Count;
            //var columns = grid.ColumnDefinitions.Count;

            //for (int i = 0; i < rows; i++)
            //{
            //    for (int j = 0; j < columns; j++)
            //    {
            //        var border = new Border()
            //        {
            //            BorderBrush = new SolidColorBrush(Colors.Gray),
            //            BorderThickness = new Thickness(1)
            //        };
            //        Grid.SetRow(border, i);
            //        Grid.SetColumn(border, j);
            //        grid.Children.Add(border);
            //    }
            //}
            #endregion

            #region 第二版：支持单元格合并，但是无法设置grid cell内元素与边框的距离
            //var controls = grid.Children;
            //var count = controls.Count;
            //for (int i = 0; i < count; i++)
            //{
            //    var item = controls[i] as FrameworkElement;
            //    var border = new Border()
            //    {
            //        BorderBrush = new SolidColorBrush(Colors.YellowGreen),
            //        BorderThickness = new Thickness(1)
            //    };

            //    var row = Grid.GetRow(item);
            //    var column = Grid.GetColumn(item);
            //    var rowspan = Grid.GetRowSpan(item);
            //    var columnspan = Grid.GetColumnSpan(item);

            //    Grid.SetRow(border, row);
            //    Grid.SetColumn(border, column);
            //    Grid.SetRowSpan(border, rowspan);
            //    Grid.SetColumnSpan(border, columnspan);

            //    grid.Children.Add(border);

            //}
            #endregion

            #region 第三版：支持grid cell元素与边框距离设置，但是方法是将cell内元素放到border中，先删除，再添加！


            int rowCount = grid.RowDefinitions.Count;
            int colCount = grid.ColumnDefinitions.Count;

            HashSet<Point> set = new HashSet<Point>();
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < colCount; col++)
                {
                    set.Add(new Point(row, col));
                }
            }

            var controls = grid.Children;
            var count = controls.Count;

            var settingThickness = GetGridLineThickness(grid);
            var gridCellPadding = GetGridCellPadding(grid);
            var gridLineBrush = GetGridLineBrush(grid);

            for (int i = 0; i < count; i++)
            {
                var item = controls[i] as FrameworkElement;
                var row = Grid.GetRow(item);
                var column = Grid.GetColumn(item);
                var rowspan = Grid.GetRowSpan(item);
                var columnspan = Grid.GetColumnSpan(item);

                for (int x = row; x < row + rowspan; x++)
                {
                    for (int y = column; y < column + columnspan; y++)
                    {
                        set.Remove(new Point(x, y));
                    }
                }

                Thickness thickness = new Thickness(0, 0, settingThickness, settingThickness);

                if (row == 0)
                    thickness.Top = settingThickness;
                if (row + rowspan == row_count)
                    thickness.Bottom = settingThickness;
                if (column == 0)
                    thickness.Left = settingThickness;
                if (column + columnspan == column_count)
                    thickness.Right = settingThickness;

                var border = new Border()
                {
                    BorderBrush = gridLineBrush,
                    BorderThickness = thickness,
                    Padding = gridCellPadding
                };

                Grid.SetRow(border, row);
                Grid.SetColumn(border, column);
                Grid.SetRowSpan(border, rowspan);
                Grid.SetColumnSpan(border, columnspan);

                grid.Children.RemoveAt(i);
                border.Child = item;
                grid.Children.Insert(i, border);
            }

            // 在没有控件的地方添加边框
            foreach(var pos in set)
            {
                Thickness thickness = new Thickness(0, 0, settingThickness, settingThickness);

                if (pos.X == 0)
                    thickness.Top = settingThickness;
                if (pos.Y == 0)
                    thickness.Left = settingThickness;

                var border = new Border()
                {
                    BorderBrush = gridLineBrush,
                    BorderThickness = thickness,
                    Padding = gridCellPadding
                };
                Grid.SetRow(border, (int)pos.X);
                Grid.SetColumn(border, (int)pos.Y);
                grid.Children.Add(border);
            }

            #endregion
        }
    }
}
