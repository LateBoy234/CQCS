using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using LiveCharts;
using LiveCharts.Charts;
using LiveCharts.Definitions.Charts;
using LiveCharts.Dtos;
using LiveCharts.Wpf;

namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    /// <summary>
    /// Defines a visual element, a visual element is a UI element that is placed and scaled in the chart.
    /// </summary>
    public class MyVisualElement : FrameworkElement, ICartesianVisualElement
    {
        private ChartCore _owner;

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Gets or sets the user interface element.
        /// </summary>
        public FrameworkElement UIElement { get; set; }
        /// <summary>
        /// Gets or sets the index of the axis in X that owns the element, the axis position must exist.
        /// </summary>
        public int AxisX { get; set; }
        /// <summary>
        /// Gets or sets the index of the axis in Y that owns the element, the axis position must exist.
        /// </summary>
        public int AxisY { get; set; }

        /// <summary>
        /// The x property
        /// </summary>
        public static readonly DependencyProperty XProperty = DependencyProperty.Register(
            "X", typeof (double), typeof (MyVisualElement), 
            new PropertyMetadata(default(double), PropertyChangedCallback));
        /// <summary>
        /// Gets or sets the X value of the UiElement
        /// </summary>
        public double X
        {
            get { return (double) GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        /// <summary>
        /// The y property
        /// </summary>
        public static readonly DependencyProperty YProperty = DependencyProperty.Register(
            "Y", typeof (double), typeof (MyVisualElement), 
            new PropertyMetadata(default(double), PropertyChangedCallback));
        /// <summary>
        /// Gets or sets the Y value of the UiElement
        /// </summary>
        public double Y
        {
            get { return (double) GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        /// <summary>
        /// The WidthByUnits property
        /// </summary>
        public static readonly DependencyProperty WidthByUnitsProperty = DependencyProperty.Register(
            "WidthByUnits", typeof(double), typeof(MyVisualElement),
            new PropertyMetadata(double.NaN, PropertyChangedCallback));

        /// <summary>
        /// UiElement的坐标系为参考的宽度
        /// </summary>
        public double WidthByUnits
        {
            get { return (double) GetValue(WidthByUnitsProperty); }
            set {SetValue(WidthByUnitsProperty, value);}
        }

        /// <summary>
        /// The HeightByUnits property
        /// </summary>
        public static readonly DependencyProperty HeightByUnitsProperty = DependencyProperty.Register(
            "HeightByUnits", typeof(double), typeof(MyVisualElement),
            new PropertyMetadata(double.NaN, PropertyChangedCallback));

        /// <summary>
        /// UiElement的坐标系为参考的宽度
        /// </summary>
        public double HeightByUnits
        {
            get { return (double) GetValue(HeightByUnitsProperty); }
            set {SetValue(HeightByUnitsProperty, value);}
        }


        /// <summary>
        /// Adds the or move.
        /// </summary>
        /// <param name="chart">The chart.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// </exception>
        public void AddOrMove(ChartCore chart)
        {
            if (chart == null || UIElement == null) return;
            if (!chart.AreComponentsLoaded) return;

            if (UIElement.Parent == null)
            {
                chart.View.AddToDrawMargin(UIElement);
            }

            var coordinate = new CorePoint(ChartFunctions.ToDrawMargin(X, AxisOrientation.X, chart, AxisX),
                ChartFunctions.ToDrawMargin(Y, AxisOrientation.Y, chart.View.Model, AxisY));

            var wpfChart = (CartesianChart) chart.View;

            var uw = new CorePoint(
                wpfChart.AxisX[AxisX].Model.EvaluatesUnitWidth
                    ? ChartFunctions.GetUnitWidth(AxisOrientation.X, chart, AxisX)/2
                    : 0,
                wpfChart.AxisY[AxisY].Model.EvaluatesUnitWidth
                    ? ChartFunctions.GetUnitWidth(AxisOrientation.Y, chart, AxisY)/2
                    : 0);

            coordinate += uw;

            if (!double.IsNaN(WidthByUnits))
            {
                var right = X + WidthByUnits;
                UIElement.Width = Math.Abs(ChartFunctions.ToDrawMargin(right, AxisOrientation.X, chart, AxisX) -
                                           ChartFunctions.ToDrawMargin(X, AxisOrientation.X, chart, AxisX));
            }

            if (!double.IsNaN(HeightByUnits))
            {
                var top = Y + HeightByUnits;
                UIElement.Height = Math.Abs(ChartFunctions.ToDrawMargin(top, AxisOrientation.Y, chart.View.Model, AxisY)
                    - ChartFunctions.ToDrawMargin(Y, AxisOrientation.Y, chart.View.Model, AxisY));
            }

            UIElement.UpdateLayout();

            switch (VerticalAlignment)
            {
                case VerticalAlignment.Top:
                    coordinate = new CorePoint(coordinate.X, coordinate.Y - UIElement.ActualHeight);
                    break;
                case VerticalAlignment.Center:
                    coordinate = new CorePoint(coordinate.X, coordinate.Y - UIElement.ActualHeight / 2);
                    break;
                case VerticalAlignment.Bottom:
                    coordinate = new CorePoint(coordinate.X, coordinate.Y);
                    break;
                case VerticalAlignment.Stretch:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    coordinate = new CorePoint(coordinate.X - UIElement.ActualWidth, coordinate.Y);
                    break;
                case HorizontalAlignment.Center:
                    coordinate = new CorePoint(coordinate.X - UIElement.ActualWidth / 2, coordinate.Y);
                    break;
                case HorizontalAlignment.Right:
                    coordinate = new CorePoint(coordinate.X, coordinate.Y);
                    break;
                case HorizontalAlignment.Stretch:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (chart.View.DisableAnimations)
            {
                Canvas.SetLeft(UIElement, coordinate.X);
                Canvas.SetTop(UIElement, coordinate.Y);
            }
            else
            {
                if (double.IsNaN(Canvas.GetLeft(UIElement)))
                {
                    Canvas.SetLeft(UIElement, coordinate.X);
                    Canvas.SetTop(UIElement, coordinate.Y);
                }
                UIElement.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(coordinate.X, wpfChart.AnimationsSpeed));
                UIElement.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(coordinate.Y, wpfChart.AnimationsSpeed));
            }

            _owner = chart;
        }

        /// <summary>
        /// Removes the specified chart.
        /// </summary>
        /// <param name="chart">The chart.</param>
        public void Remove(ChartCore chart)
        {
            chart.View.RemoveFromDrawMargin(UIElement);
        }

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var element = (MyVisualElement) dependencyObject;
            if (element._owner != null) element.AddOrMove(element._owner);
        }
    }
}
