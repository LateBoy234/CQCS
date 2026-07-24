using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ThreeNH.ColorQualityControl
{
    public static class GridExtensions
    {
        /// <summary>
        /// 获取Grid控件指定行列处的控件
        /// </summary>
        /// <param name="grid">Grid控件</param>
        /// <param name="row">控件所在行</param>
        /// <param name="col">控件所在列</param>
        /// <returns>返回指定行列的控件</returns>
        public static UIElement GetItemAt(this Grid grid, int row, int col)
        {
            return grid.Children.Cast<UIElement>().FirstOrDefault(
                e => Grid.GetRow(e) == row && Grid.GetColumn(e) == col);
        }

        /// <summary>
        /// 移除Grid指定行列的控件
        /// </summary>
        /// <param name="grid">Grid控件</param>
        /// <param name="row">要移除的控件所在行</param>
        /// <param name="col">要移除的控件所在列</param>
        public static void RemoveItemAt(this Grid grid, int row, int col)
        {
            UIElement e = GetItemAt(grid, row, col);
            if (e != null)
            {
                grid.Children.Remove(e);
            }
        }

        /// <summary>
        /// 移除指定行并其行所有控件一起移除
        /// </summary>
        /// <param name="grid">Grid控件</param>
        /// <param name="rowStart">要移除行的起始行</param>
        /// <param name="count">移除的行数</param>
        public static void RemoveRowRange(this Grid grid, int rowStart, int count)
        {
            int columnCount = grid.ColumnDefinitions.Count;
            for (int i = 0; i < count; i++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    RemoveItemAt(grid, rowStart + i, col);
                }
            }
            grid.RowDefinitions.RemoveRange(rowStart, count);
        }
    }
}