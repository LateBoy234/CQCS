using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ThreeNH.Controls
{
    public class GridWithLine : Grid
    {
        public static readonly DependencyProperty ShowCellLinesProperty =
            DependencyProperty.Register("ShowCellLines", typeof(bool),
                typeof(GridWithLine), new FrameworkPropertyMetadata(false,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 是否显示网格线
        /// </summary>
        public bool ShowCellLines
        {
            get { return (bool)GetValue(ShowCellLinesProperty); }
            set { SetValue(ShowCellLinesProperty, value); }
        }

        public static readonly DependencyProperty CellLineStrokeProperty =
            DependencyProperty.Register("CellLineStroke", typeof(Brush),
                typeof(GridWithLine), new FrameworkPropertyMetadata(Brushes.Black,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        
        public Brush CellLineStroke
        {
            get { return (Brush)GetValue(CellLineStrokeProperty); }
            set { SetValue(CellLineStrokeProperty, value); }
        }

        public static readonly DependencyProperty CellLineThicknessProperty =
            DependencyProperty.Register("CellLineThickness", typeof(double),
                typeof(GridWithLine), new FrameworkPropertyMetadata(1.0,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public double CellLineThickness
        {
            get { return (double)GetValue(CellLineThicknessProperty); }
            set { SetValue(CellLineThicknessProperty, value); }
        }

        public static readonly DependencyProperty CellPaddingProperty =
            DependencyProperty.Register("CellPadding", typeof(Thickness),
                typeof(GridWithLine), new FrameworkPropertyMetadata(new Thickness(0),
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public Thickness CellPadding
        {
            get { return (Thickness)GetValue(CellPaddingProperty); }
            set { SetValue(CellPaddingProperty, value); }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var size = base.MeasureOverride(constraint);
            InvalidateVisual();  // 强迫重绘
            return size;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            if (ShowCellLines)
            {
                int rowCount = RowDefinitions.Count;
                int colCount = ColumnDefinitions.Count;

                HashSet<Point> set = new HashSet<Point>();
                for (int row = 0; row < rowCount; row++)
                {
                    for (int col = 0; col < colCount; col++)
                    {
                        set.Add(new Point(col, row));
                    }
                }


                double[] rowTops = new double[rowCount + 1]; // 所有行的Y坐标
                rowTops[0] = 0.0;
                for (int row = 0; row < rowCount; row++)
                {
                    rowTops[row + 1] = rowTops[row] + RowDefinitions[row].ActualHeight;
                }

                double[] columnLefts = new double[colCount + 1];  // 所有列的X坐标
                columnLefts[0] = 0.0;
                for (int col = 0; col < colCount; col++)
                {
                    columnLefts[col + 1] = columnLefts[col] + ColumnDefinitions[col].ActualWidth;
                }

                Pen pen = new Pen(CellLineStroke, CellLineThickness);
                dc.DrawLine(pen, new Point(0, 0), new Point(columnLefts[colCount], 0));
                dc.DrawLine(pen, new Point(0, 0), new Point(0, rowTops[rowCount]));

                //foreach (UIElement child in Children)
                for (int i = 0; i < VisualChildrenCount; i++)
                {
                    UIElement child = GetVisualChild(i) as UIElement;
                    var row = GetRow(child);
                    var column = GetColumn(child);
                    var rowspan = GetRowSpan(child);
                    var columnspan = GetColumnSpan(child);

                    for (int irow = row; irow < row + rowspan; irow++)
                    {
                        for (int icol = column; icol < column + columnspan; icol++)
                        {
                            set.Remove(new Point(icol, irow));
                        }
                    }

                    double width = columnLefts[column + columnspan] - columnLefts[column];
                    double height = rowTops[row + rowspan] - rowTops[row];
                    if (width > 0 && height > 0)
                    {
                        dc.DrawLine(pen, new Point(columnLefts[column + columnspan], rowTops[row]),
                            new Point(columnLefts[column + columnspan], rowTops[row + rowspan]));
                        dc.DrawLine(pen, new Point(columnLefts[column], rowTops[row + rowspan]),
                            new Point(columnLefts[column + columnspan], rowTops[row + rowspan]));
                    }
                }


                // 在没有控件的地方添加边框
                foreach (var pos in set)
                {
                    int column = (int)pos.X;
                    int row = (int)pos.Y;

                    double width = columnLefts[column + 1] - columnLefts[column];
                    double height = rowTops[row + 1] - rowTops[row];
                    if (width > 0 && height > 0)
                    {
                        dc.DrawLine(pen, new Point(columnLefts[column + 1], rowTops[row]),
                            new Point(columnLefts[column + 1], rowTops[row + 1]));
                        dc.DrawLine(pen, new Point(columnLefts[column], rowTops[row + 1]),
                            new Point(columnLefts[column + 1], rowTops[row + 1]));
                    }
                }
            }
        }
    }
}
