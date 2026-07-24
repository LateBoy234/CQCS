using System.Windows.Input;
using ThreeNH.ColorQualityControl.UI.ColourAtla;
using ThreeNH.DataModel;
using ThreeNH.Optical;

namespace ThreeNH.ColorQualityControl
{
    public class CurrentSetting
    {
        public SpecularComponent SpecCom;
        public ObserverIlluminantPair IlluObs;
        public ChromaticitySetting DispSet;
        public SystemToleranceSetting ToleranceSet;
        public StandardSpecimen CurrentStandard;
        public MainWindow MainWindowObj;
    }

    public partial class MainWindow
    {
        private void MatchAndSearchExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentSetting curSet = new CurrentSetting
            {
                SpecCom = SpecularComponent,
                IlluObs = ObserverIlluminant,
                DispSet = SystemDefaultConfig.DisplaySetting,
                ToleranceSet = SystemDefaultConfig.ToleranceSetting,
                CurrentStandard = CurrentStandard,
                MainWindowObj = this,
            };
            PanTongMatch mat = new PanTongMatch(curSet, Colorimeter);
            mat.ShowDialog();
        }
    }
}