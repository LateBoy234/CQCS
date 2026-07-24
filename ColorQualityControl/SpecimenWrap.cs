using System;
using System.ComponentModel;
using System.Windows.Media;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorQualityControl.Adapter;
using ThreeNH.ColorSpecimen;

namespace ThreeNH.ColorQualityControl
{
    public class SpecimenWrap : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SpecimenWrap() { }

        public SpecimenWrap(SpecimenAdapter adapter)
        {
            this.Adapter = adapter;
        }

        private SpecimenAdapter _adapter = null;
        public SpecimenAdapter Adapter
        {
            get { return _adapter; }
            set
            {
                if (_adapter != value)
                {
                    _adapter = value;
                }
                if (_adapter != null)
                {
                    NotifyDataUpdated();
                }
            }
        }

        public void NotifyDataUpdated()
        {
            NotifyPropertyChanged("Adapter");
            NotifyPropertyChanged("Spectrum");
            NotifyPropertyChanged("PseudoColor");
            NotifyPropertyChanged("Judgement");
        }

        public Specimen Specimen
        {
            get
            {
                return Adapter.Specimen;
            }
        }

        public Spectrum Spectrum
        {
            get
            {
                return Adapter.Spectrum;
            }
        }

        public string Name
        {
            get
            {
                return Specimen.Name;
            }
            set
            {
                Specimen.Name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public string Material
        {
            get
            {
                return Specimen.Material;
            }
            set
            {
                Specimen.Material = value;
                NotifyPropertyChanged("Material");
            }
        }

        public Brush PseudoColor
        {
            get { return Adapter.PseudoColorBrush; }
        }

        public Judgement Judgement
        {
            get
            {
                if (Adapter is TestSpecimenAdapter)
                    return (Adapter as TestSpecimenAdapter).Judgement;
                return null;
            }
        }

        public int ID
        {
            get { return Specimen.ID; }
        }

        private object _tag;
        public object Tag
        {
            get { return _tag; }
            set
            {
                if (_tag != value)
                {
                    _tag = value;
                    NotifyPropertyChanged("Tag");
                }
            }
        }
    }
}
