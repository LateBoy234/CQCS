using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.ColorSpecimen;

namespace ThreeNH.ColorimeterInterface
{
    /// <summary>
    /// 存样库管理接口
    /// </summary>
    public interface ITargetLibraryManagement
    {
        /// <summary>
        /// 获取存样数
        /// </summary>
        int GetTargetCount();

        /// <summary>
        /// 写入存样库
        /// </summary>
        void WriteToLibrary(IEnumerable<Specimen> targets);

        /// <summary>
        /// 读取存样
        /// </summary>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        Specimen[] ReadTargets(int from = 0, int count = -1);

        /// <summary>
        /// 清空存样库
        /// </summary>
        void ClearLibrary();
    }
}
