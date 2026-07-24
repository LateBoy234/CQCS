using System;
using System.Windows.Input;
using ThreeNH.ColorQualityControl.UI;

namespace ThreeNH.ColorQualityControl
{
    public partial class MainWindow
    {
        private void WhitenessIndexExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            WhitenessDialog dlg = new WhitenessDialog();
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        private void MetamericIndexExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            MetamerismIndiexDialog dlg = new MetamerismIndiexDialog(this);
            dlg.ShowDialog();
        }

        private void FastnessIndexExecuted(object sender, ExecutedRoutedEventArgs e)
        { 
        }


        private void StrengthIndexExecuted(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void YellownessIndexExecuted(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void OpacityExecuted(object sender, ExecutedRoutedEventArgs e)
        {
        }
    }
}
