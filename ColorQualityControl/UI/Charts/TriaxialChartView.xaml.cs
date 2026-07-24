using LiveCharts;
using LiveCharts.Wpf;
using System.Windows;
using System.Windows.Controls;

namespace ThreeNH.ColorQualityControl.UI.Charts
{
    /// <summary>
    /// TriaxialChartView.xaml 的交互逻辑
    /// </summary>
    public partial class TriaxialChartView : UserControl
    {
        public TriaxialChartView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += (s, e) => ApplyTooltip(DataContext);
        }

        void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ApplyTooltip(e.NewValue);
        }

        /// <summary>
        /// 为每个图表创建独立 Tooltip，并限定为 OnlySender，避免 DataMouseEnter 空引用。
        /// </summary>
        void ApplyTooltip(object dataContext)
        {
            if (dataContext == null)
                return;

            var tooltipKey = dataContext is ViewModels.Charts.TriaxialChartViewModel vm && vm.UsePlainTooltip
                ? "PlainTooltip"
                : "DataTooltip";

            XyChart.DataTooltip = CreateTooltip(tooltipKey);
            ZChart.DataTooltip = CreateTooltip(tooltipKey);
        }

        DefaultTooltip CreateTooltip(string key)
        {
            var tooltip = (DefaultTooltip)FindResource(key);
            tooltip.SelectionMode = TooltipSelectionMode.OnlySender;
            return tooltip;
        }

        public void Clear()
        {
            
        }
    }
}
