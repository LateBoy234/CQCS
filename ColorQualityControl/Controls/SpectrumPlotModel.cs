using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OxyPlot;
using OxyPlot.Series;
using ThreeNH.ColorAlgorithm;
using System.Windows.Media;

namespace ThreeNH.Controls
{
    class SpectrumPlotModel : PlotModel
    {
        public void AddStandard(Spectrum spectrum, string name, Brush stroke = null)
        {
            var series = MakeSeries(spectrum, name);
            series.Stroke = stroke == null ? Brushes.DarkRed : stroke;
            _StandardLineSeries.Add(series);
            chart.Series.Add(series);

            AdjustRange(spectrum);
        }

        public void AddTest(Spectrum spectrum, string name, Brush stroke = null)
        {
            var series = MakeSeries(spectrum, name);
            series.Stroke = stroke == null ? Brushes.Gray : stroke;
            _TestLineSeries.Add(series);
            chart.Series.Add(series);

            AdjustRange(spectrum);
        }

        private LineSeries MakeSeries(Spectrum spectrum, string name)
        {
            LineSeries series = new LineSeries();

            for (int wave = spectrum.Range.Min; wave <= spectrum.Range.Max;
                wave += spectrum.Range.Step)
            {
                series.Points.Add(new DataPoint(wave, spectrum[wave] * 100.0));
            }

            //series.Values = values;
            series.Title = name;
            series.Tag = spectrum;
            series.StrokeThickness = 1;
            series.LabelFormatString = "F2";
            series.LabelPoint = pt => string.Format("{0:F2}", pt.Y);
            if (!IsFillSeries)
                series.Fill = Brushes.Transparent;

            return series;
        }
    }
}
