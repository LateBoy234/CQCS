using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace ThreeNH.ColorQualityControl
{
    static class Commands
    {
        /// <summary>
        /// 连接仪器
        /// </summary>
        public static RoutedUICommand Connect { get; private set; }
        /// <summary>
        /// 通过蓝牙连接仪器
        /// </summary>
        public static RoutedUICommand ConnectByBluetooth { get; private set; }
        /// <summary>
        /// 断开连接
        /// </summary>
        public static RoutedUICommand Disconnect { get; private set; }
        /// <summary>
        /// 测量标样
        /// </summary>
        public static RoutedUICommand MeasureStandard { get; private set; }
        /// <summary>
        /// 测量试样
        /// </summary>
        public static RoutedUICommand MeasureTrial { get; private set; }
        /// <summary>
        /// 观察者角度和光源设置
        /// </summary>
        public static RoutedUICommand ObserverAndIlluminant { get; private set; }
        /// <summary>
        /// 颜色空间及色差属性设置
        /// </summary>
        public static RoutedUICommand ChromaticitySetting { get; private set; }
        /// <summary>
        /// 容差设置
        /// </summary>
        public static RoutedUICommand ToleranceSetting { get; private set; }
        /// <summary>
        /// 比色皿光程
        /// </summary>
        public static RoutedUICommand PathlenthSelect { get; private set; }
        /// <summary>
        /// 测试模式设置：单次测量/连续测量/平均测量
        /// </summary>
        public static RoutedUICommand TestModeSetting { get; private set; }
        /// <summary>
        /// 仪器状态
        /// </summary>
        public static RoutedUICommand InstrumentStatus { get; private set; }
        /// <summary>
        /// 仪器数据管理
        /// </summary>
        public static RoutedUICommand InstrumentDataManagement { get; private set; }
        /// <summary>
        /// 仪器配置
        /// </summary>
        public static RoutedUICommand InstrumentConfiguration { get; private set; }
        /// <summary>
        /// 向仪器中写入标样
        /// </summary>
        public static RoutedUICommand WriteStandardToInstrument { get; private set; }
        /// <summary>
        /// 輸入標樣
        /// </summary>
        public static RoutedUICommand InputStandard { get; private set; }
        /// <summary>
        /// 白度指數
        /// </summary>
        public static RoutedUICommand WhitenessIndex { get; private set; }
        /// <summary>
        /// 黄度指数
        /// </summary>
        public static RoutedUICommand YellownessIndex { get; private set; }
        /// <summary>
        /// 牢度指数
        /// </summary>
        public static RoutedUICommand FastnessIndex { get; private set; }
        /// <summary>
        /// 力份
        /// </summary>
        public static RoutedUICommand StrengthIndex { get; private set; }
        /// <summary>
        /// 同色异谱指数
        /// </summary>
        public static RoutedUICommand MetamericIndex { get; private set; }
        /// <summary>
        /// 遮盖度指数
        /// </summary>
        public static RoutedUICommand Opacity { get; private set; }
        /// <summary>
        /// 高级测量
        /// </summary>
        public static RoutedUICommand AdventureMeasure { get; private set; }
        /// <summary>
        /// 标样高级测量
        /// </summary>
        public static RoutedUICommand StandardAdventureMeasure { get; private set; }
        /// <summary>
        /// 试样高级测量
        /// </summary>
        public static RoutedUICommand TestAdventureMeasure { get; private set; }

        /// <summary>
        /// 设置标样的容差
        /// </summary>
        public static RoutedUICommand StandardToleranceSetting { get; private set; }
        /// <summary>
        /// 设置标样的显示项
        /// </summary>
        public static RoutedUICommand StandardChromaticitySetting { get; private set; }
        /// <summary>
        /// 设为当前标样
        /// </summary>
        public static RoutedUICommand SetAsCurrentStandard { get; private set; }
        /// <summary>
        /// 重命名
        /// </summary>
        public static RoutedUICommand Rename { get; private set; }
        /// <summary>
        /// 修改料号
        /// </summary>
        public static RoutedUICommand ModifyMaterial { get; private set; }
        /// <summary>
        /// 设置命名规则
        /// </summary>
        public static RoutedUICommand SetRulesOfAutoNaming { get; private set; }
        /// <summary>
        /// 将试样移动到标样列表中
        /// </summary>
        public static RoutedUICommand MoveToStandard { get; private set; }
        /// <summary>
        /// 将标样移到试样列表中
        /// </summary>
        public static RoutedUICommand CopyToTest { get; private set; }

        /// <summary>
        /// 将标样移到试样列表中
        /// </summary>
        public static RoutedUICommand CopyToUnderAStandard { get; private set; }

        /// <summary>
        /// 更改试样关联的标样
        /// </summary>
        public static RoutedUICommand ChangeStandard { get; private set; }
        /// <summary>
        /// 导出所有数据到Excel或文本文件
        /// </summary>
        public static RoutedUICommand ExportAll { get; private set; }
        /// <summary>
        /// 所选标样及其下的试样
        /// </summary>
        public static RoutedUICommand ExportSelectedWithTests { get; private set; }
        /// <summary>
        /// 导出所选样品数据
        /// </summary>
        public static RoutedUICommand ExportSelected { get; private set; }
        /// <summary>
        /// 打印设置
        /// </summary>
        public static RoutedUICommand PrinterSetup { get; private set; }
        /// <summary>
        /// 黑白校正
        /// </summary>
        public static RoutedUICommand DoCalibration { get; private set; }
        /// <summary>
        /// 关于
        /// </summary>
        public static RoutedUICommand About { get; private set; }

        /// <summary>
        /// 更新
        /// </summary>
        public static RoutedUICommand Update { get; private set; }

        /// <summary>
        /// 打印到Word
        /// </summary>
        public static RoutedUICommand PrintToWord { get; private set; }

        /// <summary>
        /// 打印到PDF
        /// </summary>
        public static RoutedUICommand PrintToPDF { get; private set; }

        /// <summary>
        /// 设置报表信息
        /// </summary>
        public static RoutedUICommand SetReportInfo { get; private set; }

        public static RoutedUICommand AutoSelectStandardSetting { get; private set; }

        public static RoutedUICommand AutoMatchStandard { get; private set; }

        public static RoutedUICommand GotoStandard { get; private set; }

        public static RoutedUICommand SwitchToStandard { get; private set; }

        public static RoutedUICommand ShowAssociatedTests { get; private set; }

        //        public static RoutedUICommand NewLibrary { get; private set; }
        public static RoutedUICommand ImportLibrary { get; private set; }
        public static RoutedUICommand ManageLibrary { get; private set; }
        public static RoutedUICommand AddToLibrary { get; private set; }
        /// <summary>
        /// 从Excel导入
        /// </summary>
        public static RoutedUICommand ImportFromExcel { get; private set; }
        /// <summary>
        /// 从文件导入标样
        /// </summary>
        public static RoutedUICommand ImportFromFile { get; }

        public static RoutedUICommand ExportOneNMSpectrum { get; }
        public static RoutedUICommand ExportFiveNMSpectrum { get; }
        /// <summary>
        /// 导入其他产家测量的光谱数据，计算Lab值，然后再导出。
        /// </summary>
        public static RoutedUICommand ConvertWhiteBoardData { get; }

        /// <summary>
        /// 仪器测量参数
        /// </summary>
        public static RoutedUICommand InstrumentMeasurementConfigs { get; }

        /// <summary>
        /// 将样品写入仪器存样库
        /// </summary>
        public static RoutedUICommand WriteToInstrumentLibrary { get; }

        /// <summary>
        /// 清空样品存样库
        /// </summary>
        public static RoutedUICommand ClearInstrumentLibrary { get; }

        /// <summary>
        /// 读取仪器存样库样品
        /// </summary>
        public static RoutedUICommand ReadInstrumentLibrary { get; }

        /// <summary>
        /// 更新仪器存样库
        /// </summary>
        public static RoutedUICommand UpdateInstrumentLibrary { get; }

        /// <summary>
        /// 从存样库查找接近的颜色
        /// </summary>
        public static RoutedUICommand FindClosedColorsFromLibrary { get; }

        /// <summary>
        /// 潘通色卡匹配检索功能
        /// </summary>
        public static RoutedUICommand PanTongMatchSearchLilin { get; }

        /// <summary>打开扫码输入窗口</summary>
        public static RoutedUICommand OpenScanBarcodeDialog { get; private set; }

        /// <summary>云端 Web API 配置</summary>
        public static RoutedUICommand CloudSetting { get; private set; }

        public static RoutedUICommand StandardSampleReport { get; private set; }

        public static RoutedUICommand PanTongMatch { get; private set; }

        public static RoutedUICommand TestSampleReport { get; private set; }

        static Commands()
        {
            Connect = new RoutedUICommand(_("MenuItem_Connect"), "Connect",
                typeof(Commands));
            ConnectByBluetooth = new RoutedUICommand(_("MenuItem_ConnectByBluetooth"),
                "ConnectByBlueTooth", typeof(Commands));
            Disconnect = new RoutedUICommand(_("MenuItem_Disconnect"), "Disconnect",
                typeof(Commands));
            MeasureStandard = new RoutedUICommand(_("MenuItem_MeasureStandard"),
                "MeasureStandard", typeof(Commands), MakeKeyGesture(Key.F5));
            MeasureTrial = new RoutedUICommand(_("MenuItem_MeasureTest"),
                "MeasureTrial", typeof(Commands), MakeKeyGesture(Key.Space));
            ObserverAndIlluminant = new RoutedUICommand(_("MenuItem_ObserverAndIlluminant"),
                "ObserverAndIlluminant", typeof(Commands));
            ChromaticitySetting = new RoutedUICommand(_("MenuItem_ChromaticitySetting"),
                "ChromaticitySetting", typeof(Commands));
            ToleranceSetting = new RoutedUICommand(_("MenuItem_ToleranceSetting"),
                "ToleranceSetting", typeof(Commands));
            InstrumentStatus = new RoutedUICommand(_("MenuItem_InstrumentStatus"),
                "InstrumentStatus", typeof(Commands));
            InstrumentDataManagement = new RoutedUICommand(_("MenuItem_InstrumentDataManagement"),
                "InstrumentDataManagement", typeof(Commands));
            InstrumentConfiguration = new RoutedUICommand(_("MenuItem_InstrumentConfiguration"),
                "InstrumentConfiguration", typeof(Commands));
            WriteStandardToInstrument = new RoutedUICommand(_("MenuItem_WriteStandardToInstrument"),
                "WriteStandardToInstrument", typeof(Commands));
            InputStandard = new RoutedUICommand(_("MenuItem_InputStandard"),
                "InputStandard", typeof(Commands));
            WhitenessIndex = new RoutedUICommand(_("MenuItem_WhitenessIndex"),
                "WhitenessIndex", typeof(Commands));
            YellownessIndex = new RoutedUICommand(_("MenuItem_YellownessIndex"),
                "YellownessIndex", typeof(Commands));
            FastnessIndex = new RoutedUICommand(_("MenuItem_FastnessIndex"),
                "FastnessIndex", typeof(Commands));
            StrengthIndex = new RoutedUICommand(_("MenuItem_StrengthIndex"),
                "StrengthIndex", typeof(Commands));
            MetamericIndex = new RoutedUICommand(_("MenuItem_MetamericIndex"),
                "MetamericIndex", typeof(Commands));
            Opacity = new RoutedUICommand(_("MenuItem_Opacity"),
                "Opacity", typeof(Commands));
            StandardToleranceSetting = new RoutedUICommand(_("MenuItem_StandardToleranceSetting"),
                "StandardToleranceSetting", typeof(Commands));
            StandardChromaticitySetting = new RoutedUICommand(_("MenuItem_StandardChromaticitySetting"),
                "StandardChromaticitySetting", typeof(Commands));
            SetAsCurrentStandard = new RoutedUICommand(_("MenuItem_SetAsCurrentStandard"),
                "SetAsCurrentStandard", typeof(Commands));
            PathlenthSelect = new RoutedUICommand(_("MenuItem_PathlenthSelect"),
              "PathlenthSelect", typeof(Commands));
            TestModeSetting = new RoutedUICommand(_("MenuItem_TestModeSetting"),
              "TestModeSetting", typeof(Commands));
            Rename = new RoutedUICommand(_("MenuItem_Rename"),
                "Rename", typeof(Commands));
            ModifyMaterial = new RoutedUICommand(_("MenuItem_ModifyMaterial"),
                "Material", typeof(Commands));
            SetRulesOfAutoNaming = new RoutedUICommand(_("MenuItem_SetAutoNamingRules"),
                "SetRulesOfAutoNaming", typeof(Commands));
            MoveToStandard = new RoutedUICommand(_("MenuItem_MoveToStandard"),
                "MoveToStandard", typeof(Commands));
            CopyToTest = new RoutedUICommand(_("MenuItem_CopyToTest"),
                "CopyToTest", typeof(Commands));
            CopyToUnderAStandard = new RoutedUICommand(_("MenuItem_CopyToUnderAStandard"),
                "CopyToUnderAStandard", typeof(Commands));
            ChangeStandard = new RoutedUICommand(_("MenuItem_ChangeStandard"),
                "ChangeStandard", typeof(Commands));
            ExportAll = new RoutedUICommand(_("MenuItem_ExportAll"),
                "ExportAll", typeof(Commands));
            ExportSelected = new RoutedUICommand(_("MenuItem_ExportSelected"),
                "ExportSelected", typeof(Commands));
            ExportSelectedWithTests = new RoutedUICommand(_("MenuItem_ExportSelectedWithTests"),
                "ExportSelectedWithTests", typeof(Commands));
            PrinterSetup = new RoutedUICommand(_("MenuItem_PrinterSetup"),
                "PrinterSetup", typeof(Commands));
            DoCalibration = new RoutedUICommand(_("MenuItem_DoCalibration"),
                "DoCalibration", typeof(Commands));
            About = new RoutedUICommand(_("MenuItem_About"),
                "About", typeof(Commands));
            Update = new RoutedUICommand(_("Label_Update"),
               "Update", typeof(Commands));
            //OpenUserManual = new RoutedUICommand(_("OpenUserManual"),
            //    "OpenUserManual", typeof(Commands), MakeKeyGesture(Key.F1));
            StandardAdventureMeasure = new RoutedUICommand(_("MenuItem_AdventureStandardMeasurement"),
                "StandardAdventureMeasure", typeof(Commands));
            TestAdventureMeasure = new RoutedUICommand(_("MenuItem_AdventureTestMeasurement"),
                "TestAdventureMeasure", typeof(Commands));
            PrintToWord = new RoutedUICommand(_("MenuItem_PrintToWord"),
                "PrintToWord", typeof(Commands));

            PrintToPDF = new RoutedUICommand(_("MenuItem_PrintToPDF"),
                "PrintToPDF", typeof(Commands));

            SetReportInfo = new RoutedUICommand(_("MenuItem_ReportSetting"),
                "SetReportInfo", typeof(Commands));

            AutoSelectStandardSetting = new RoutedUICommand(_("MenuItem_AutoSelectStandardSetting"),
                "AutoSelectStandardSetting", typeof(Commands));

            AutoMatchStandard = new RoutedUICommand(_("MenuItem_AutoMatchStandard"),
                "AutoMatchStandard", typeof(Commands));

            GotoStandard = new RoutedUICommand(_("MenuItem_GotoStandard"), "GotoStandard", typeof(Commands)); 

            SwitchToStandard = new RoutedUICommand(_("MenuItem_SwitchToStandard"), "SwitchToStandard", typeof(Commands));

            ShowAssociatedTests = new RoutedUICommand(_("MenuItem_ShowAssociatedTests"), "ShowAssociatedTests", typeof(Commands));

            //            NewLibrary = new RoutedUICommand(_("MenuItem_NewLibrary"), "NewLibrary", typeof(Commands));
            ImportLibrary = new RoutedUICommand(_("MenuItem_ImportLibrary"), "ImportLibrary", typeof(Commands));
            AddToLibrary = new RoutedUICommand(_("MenuItem_AddToLibrary"), "AddToLibrary", typeof(Commands));
            ManageLibrary = new RoutedUICommand(_("MenuItem_ManageLibrary"), "ManageLibrary", typeof(Commands));
            ImportFromExcel = new RoutedUICommand(_("MenuItem_ImportFromExcel"), "ImportFromExcel", typeof(Commands));
            ImportFromFile = new RoutedUICommand(_("MenuItem_ImportFromFile"), "ImportFromFile", typeof(Commands));
            ExportOneNMSpectrum = new RoutedUICommand(_("MenuItem_ExportOneNMIntervalSpectrum"), "ExportOneNMSpectrum", typeof(Commands));
            ExportFiveNMSpectrum = new RoutedUICommand(_("MenuItem_ExportFiveNMIntervalSpectrum"), "ExportFiveNMSpectrum", typeof(Commands));
            ConvertWhiteBoardData = new RoutedUICommand(_("MenuItem_ConvertWhiteBoardData"), "ConvertWhiteBoardData", typeof(Commands));

            InstrumentMeasurementConfigs = new RoutedUICommand(_("MenuItem_InstrumentMeasurementConfigs"),
                "InstrumentMeasurementConfigs", typeof(Commands));

            WriteToInstrumentLibrary = new RoutedUICommand(_("MenuItem_WriteToInstrumentLibrary"),
                nameof(WriteToInstrumentLibrary), typeof(Commands));
            ClearInstrumentLibrary = new RoutedUICommand(_("MenuItem_ClearInstrumentLibrary"),
                nameof(ClearInstrumentLibrary), typeof(Commands));
            ReadInstrumentLibrary = new RoutedUICommand(_("MenuItem_ReadInstrumentLibrary"),
                nameof(ReadInstrumentLibrary), typeof(Commands));
            UpdateInstrumentLibrary = new RoutedUICommand(_("MenuItem_UpdateInstrumentLibrary"),
                nameof(UpdateInstrumentLibrary), typeof(Commands));
            FindClosedColorsFromLibrary = new RoutedUICommand(_("MenuItem_FindClosedColorsFromLibrary"),
                nameof(FindClosedColorsFromLibrary), typeof(Commands));

            PanTongMatchSearchLilin = new RoutedUICommand(_("PanTongMatchTipLilin"),
                nameof(PanTongMatchSearchLilin), typeof(Commands));
            OpenScanBarcodeDialog = new RoutedUICommand(_("MenuItem_ScanBarcode"),
                nameof(OpenScanBarcodeDialog), typeof(Commands),
                MakeKeyGesture(Key.F6));
            CloudSetting = new RoutedUICommand(_("MenuItem_CloudSetting"),
                nameof(CloudSetting), typeof(Commands));

            StandardSampleReport = new RoutedUICommand(_("SampleReport"), nameof(StandardSampleReport), typeof(Commands));
            PanTongMatch = new RoutedUICommand(_("PanTongMatchTipLilin"), nameof(PanTongMatch), typeof(Commands));
            TestSampleReport = new RoutedUICommand(_("SampleReport"), nameof(TestSampleReport), typeof(Commands));
        }


        static string _(string resourceKey)
        {
            return S._(resourceKey);
        }

        static InputGestureCollection MakeKeyGesture(Key key)
        {
            InputGestureCollection coll = new InputGestureCollection();
            coll.Add(new KeyGesture(key));
            return coll;
        }
        static InputGestureCollection MakeKeyGesture(Key key, ModifierKeys modifiers)
        {
            InputGestureCollection coll = new InputGestureCollection();
            coll.Add(new KeyGesture(key, modifiers));
            return coll;
        }
        static InputGestureCollection MakeKeyGesture(Key key, ModifierKeys modifiers,
            string displayString)
        {
            InputGestureCollection coll = new InputGestureCollection();
            coll.Add(new KeyGesture(key, modifiers, displayString));
            return coll;
        }
    }
}
