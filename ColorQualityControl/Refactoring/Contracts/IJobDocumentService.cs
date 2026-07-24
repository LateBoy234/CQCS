using System.Collections.Generic;
using ThreeNH.ColorSpecimen;
using ThreeNH.DataModel;

namespace ThreeNH.ColorQualityControl.Refactoring.Contracts
{
    /// <summary>
    /// 阶段 1 目标：Job 持久化与标试样集合（现由 MainWindow.Data / MainWindow.File 中的 Database 承担）。
    /// 关键路径：Save、LoadAllSpecimens、AddStandard、AddTest、ImportStandards。
    /// </summary>
    internal interface IJobDocumentService
    {
        SpecimenDatabase Database { get; }

        void AddStandard(StandardSpecimen standard, bool createNewId = true, bool isRemoveSame = true);

        void AddTest(TestSpecimen test, StandardSpecimen standard, bool isSelectedNewTest = false);

        void RemoveStandard(StandardSpecimen standard);

        void RemoveTest(TestSpecimen test);

        void LoadAllSpecimens();

        void Clear();
    }
}
