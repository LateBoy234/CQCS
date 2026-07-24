using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace ThreeNH.ColorQualityControl.ViewModels.Charts
{
    /// <summary>
    /// 光谱图光谱
    /// </summary>
    class SpectraChartItem
    {
        public string Name { get; set; }
        public ISpectralData SpectralData { get; set; }
        public object Tag { get; set; }

        public SpectraChartItem() { }

        public SpectraChartItem(string name, ISpectralData spectralData, object tag = null)
        {
            this.Name = name;
            this.SpectralData = spectralData;
            this.Tag = tag;
        }
    }

    /// <summary>
    /// 光谱图
    /// </summary>
    class SpectraChartViewModel : INotifyPropertyChanged
    {
        private LegendLocation _legendLocation = LegendLocation.None;
        private int _minWavelength = 400;
        private int _maxWavelength = 700;
        private int _wavelengthInterval = 50;
        private double _minSpectralValue = 0.0;
        private double _maxSpectralValue = 1.0;
        private double _spectralValueInterval = 0.2;

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SpectraChartViewModel()
        {
            SpectralCollection = new ObservableCollection<SpectraChartItem>();
            VisualElements = new VisualElementsCollection();
            SeriesCollection = new SeriesCollection();

            SpectralCollection.CollectionChanged += OnSpectralCollectionChanged;
        }

        private void OnSpectralCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var item = (SpectraChartItem)e.NewItems[0];
                AddSpectralData(item);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                var item = (SpectraChartItem)e.OldItems[0];
                var series = SeriesCollection.FirstOrDefault(s => (s as LineSeries)?.Tag == item);
                if (series != null)
                {
                    SeriesCollection.Remove(series);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                SeriesCollection.Clear();
                foreach (var item in SpectralCollection)
                {
                    AddSpectralData(item);
                }
            }
        }

        private void AddSpectralData(SpectraChartItem item)
        {
            var points = new ChartValues<ScatterPoint>();
            for (int wavelength = item.SpectralData.WavelengthBegin;
                wavelength <= item.SpectralData.WavelengthEnd;
                wavelength += item.SpectralData.WavelengthInterval)
            {
                points.Add(new ScatterPoint(wavelength, item.SpectralData[wavelength]));
            }

            var series = new LineSeries
            {
                Tag = item,
                Values = points,
                LabelPoint = pt => pt.Y.ToString("P2"),
                Title = item.Name,
            };
            SeriesCollection.Add(series);
        }

        public LineSeries FindSeries(SpectraChartItem item)
        {
            return (LineSeries)SeriesCollection.FirstOrDefault(s => (s as LineSeries)?.Tag == item);
        }

        public ObservableCollection<SpectraChartItem> SpectralCollection { get; }

        public SeriesCollection SeriesCollection { get; }

        public VisualElementsCollection VisualElements { get; }

        public LegendLocation LegendLocation
        {
            get => _legendLocation;
            set
            {
                if (value == _legendLocation) return;
                _legendLocation = value;
                RaisePropertyChange(nameof(LegendLocation));
            }
        }

        public int MinWavelength
        {
            get => _minWavelength;
            set
            {
                if (value == _minWavelength) return;
                _minWavelength = value;
                RaisePropertyChange(nameof(MinWavelength));
            }
        }

        public int MaxWavelength
        {
            get => _maxWavelength;
            set
            {
                if (value == _maxWavelength) return;
                _maxWavelength = value;
                RaisePropertyChange(nameof(MaxWavelength));
            }
        }

        public int WavelengthInterval
        {
            get => _wavelengthInterval;
            set
            {
                if (value == _wavelengthInterval) return;
                _wavelengthInterval = value;
                RaisePropertyChange(nameof(WavelengthInterval));
            }
        }

        public double MinSpectralValue
        {
            get => _minSpectralValue;
            set
            {
                if (value.Equals(_minSpectralValue)) return;
                _minSpectralValue = value;
                RaisePropertyChange(nameof(MinSpectralValue));
            }
        }

        public double MaxSpectralValue
        {
            get => _maxSpectralValue;
            set
            {
                if (value.Equals(_maxSpectralValue)) return;
                _maxSpectralValue = value;
                RaisePropertyChange(nameof(MaxSpectralValue));
            }
        }

        public double SpectralValueInterval
        {
            get => _spectralValueInterval;
            set
            {
                if (value.Equals(_spectralValueInterval)) return;
                _spectralValueInterval = value;
                RaisePropertyChange(nameof(SpectralValueInterval));
            }
        }

        public Func<double, string> SpectralLabelFormatter { get; set; } = d => d.ToString("P2");
    }
}
