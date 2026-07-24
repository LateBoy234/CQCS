using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using ThreeNH.ColorAlgorithm;
using ThreeNH.ColorimeterInterface;
using ThreeNH.ColorSpecimen;
using ThreeNH.CommunicationBase;

namespace ThreeNH.ColorQualityControl.UI
{
    class HazeMeasurementWizardPage : UserControl
    {
        private IInstrument _instrument;

        public event EventHandler<ReturnEventArgs<ColorData>> Return;

        public HazeMeasurementWizardPage(IInstrument instrument, string title, string prompt)
        {
            _instrument = instrument;

            var grid = new Grid();
            grid.Margin = new Thickness(10, 10, 10, 10);
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleTxtBlock = new TextBlock { Text = title };
            titleTxtBlock.TextAlignment = TextAlignment.Center;
            titleTxtBlock.VerticalAlignment = VerticalAlignment.Center;
            titleTxtBlock.FontSize = 14;
            titleTxtBlock.FontWeight = FontWeights.Bold;
            titleTxtBlock.TextWrapping = TextWrapping.Wrap;
            grid.Children.Add(titleTxtBlock);
            Grid.SetColumn(titleTxtBlock, 0);
            Grid.SetRow(titleTxtBlock, 0);


            var txtBlock = new TextBlock { Text = prompt };
            txtBlock.TextAlignment = TextAlignment.Left;
            txtBlock.VerticalAlignment = VerticalAlignment.Center;
            txtBlock.FontSize = 14;
            txtBlock.TextWrapping = TextWrapping.Wrap;
            grid.Children.Add(txtBlock);
            Grid.SetColumn(txtBlock, 0);
            Grid.SetRow(txtBlock, 1);

            var button = new Button { Content = S._("Measure") };
            button.Margin = new Thickness(5);
            button.Padding = new Thickness(5);
            button.IsDefault = true;
            button.MinHeight = 24;
            grid.Children.Add(button);
            Grid.SetColumn(button, 0);
            Grid.SetRow(button, 2);
            button.Click += OnMeasure;

            Content = grid;
        }

        private void OnMeasure(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {

                var button = sender as Button;
                var hazeDialog = ((((button.Parent as Grid)?.Parent as UserControl)?.Parent as DockPanel).Parent as Grid)?.Parent as HazeMeasurementDialog;
                var btnCancel = hazeDialog?.btnCancel;
                var btnRef = hazeDialog?.btnRef;
                this.Dispatcher.Invoke(new Action(() =>
                {
                    button.Content = S._("Measuring");
                    button.IsEnabled = false;
                    if (hazeDialog != null)
                    {
                        btnCancel.IsEnabled = false;
                        btnRef.IsEnabled = false;
                    }
                }));
                try
                {
                    var t = _instrument.Measure();
                    ColorData clr = t.Clr != null ? t.Clr : t.SCI;
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        Return?.Invoke(this, new ReturnEventArgs<ColorData>(clr));
                    }));

                }
                catch (CommunicationException ex)
                {
                    _instrument.Close();
                    MessageBox.Show(S._(ex.Message));
                }
                finally
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        button.IsEnabled = true;
                        if (hazeDialog != null)
                        {
                            btnCancel.IsEnabled = true;
                            btnRef.IsEnabled = true;
                        }
                    }));
                }
            });
        }
    }
}
