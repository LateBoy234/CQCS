using System.Windows;
using System.Windows.Controls;
using ThreeNH.ColorQualityControl.Adapter;

namespace ThreeNH.ColorQualityControl.UI
{
    class SpectrumTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SpectrumTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var adapter = item as SpecimenAdapter;
            if (adapter == null || adapter.Spectrum == null)
                return base.SelectTemplate(item, container);

            return SpectrumTemplate;
        }
    }
}
