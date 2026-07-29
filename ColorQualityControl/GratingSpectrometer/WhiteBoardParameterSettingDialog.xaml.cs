﻿﻿using System;
using System.Windows;
using System.Windows.Controls;
using ThreeNH.Controls;
using ThreeNH.ColorQualityControl;

namespace ThreeNH.SQCX.Infrastructure.Instruments.GratingSpectrometer
{
    /// <summary>
    /// WhiteBoardParameterControl.xaml 的交互逻辑
    /// </summary>
    public partial class WhiteBoardParameterSettingDialog : Window
    {
        public WhiteBoardParameterSettingDialog(Spectrometer spectrometer)
        {
            Spectrometer = spectrometer;
            InitializeComponent();
        }

        public Spectrometer Spectrometer
        {
            get; set;
        }

        /// <summary>
        /// 验证用户输入是否有效
        /// </summary>
        /// <returns>如果输入有效返回真；否则返回假</returns>
        public bool Verify()
        {
            InnerNumber.Text = InnerNumber.Text.Trim();
            OuterNumber.Text = OuterNumber.Text.Trim();

            if (string.IsNullOrEmpty(InnerNumber.Text))
            {
                MessageBox.Show(S._("PleaseInputInnerNumber"));
                InnerNumber.Focus();
                return false;
            }
            if (string.IsNullOrEmpty(OuterNumber.Text))
            {
                MessageBox.Show(S._("PleaseInputExternalNumber"));
                OuterNumber.Focus();
                return false;
            }
            if (!SCI.Verify() || !SCE.Verify())
            {
                return false;
            }

            

            return true;
        }

        public WhiteBoardParameter WhiteBoardParameter
        {
            get
            {
                WhiteBoardParameter para = new WhiteBoardParameter();
                para.InnerNumber = InnerNumber.Text;
                para.OuterNumber = OuterNumber.Text;
                para.SCI = SCI.Spectrum;
                para.SCE = SCE.Spectrum;

                return para;
            }
            set
            {
                if (value == null)
                    return;

                InnerNumber.Text = value.InnerNumber;
                OuterNumber.Text = value.OuterNumber;
                SCI.Spectrum = value.SCI;
                SCE.Spectrum = value.SCE;
            }
        }

        private void OnReadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                WhiteBoardParameter = Spectrometer.GetWhiteBoardParameter();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnWriteClick(object sender, RoutedEventArgs e)
        {
            if (Verify())
            {
                try
                {
                    Spectrometer.SetWhiteBoardParameter(WhiteBoardParameter);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}
