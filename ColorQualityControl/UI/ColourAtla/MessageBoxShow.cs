using System.Windows;

namespace ThreeNH.ColorQualityControl.UI.ColourAtla
{
    class MessageBoxShow
    {
        public static bool DialogSetToBeStandardEnsure(string str = null)
        {
            MessageBoxResult res = System.Windows.MessageBox.Show( str + ":" + S._("SetToBeStandardEnsureLilin"), "", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK) return true;
            return false;
        }
    }
}
