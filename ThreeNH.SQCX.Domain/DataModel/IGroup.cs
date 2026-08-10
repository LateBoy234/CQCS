using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.DataModel
{
    public interface IGroupItem
    {
        /// <summary>
        /// 名称
        /// </summary>
        string Name { get; set; }
    }

    public interface IGroup : IGroupItem
    {
        /// <summary>
        /// 组成员
        /// </summary>
        IList<IGroupItem> Items { get; }

        /// <summary>
        /// 当前项
        /// </summary>
        IGroupItem CurrentItem { get; }

        /// <summary>
        /// 所有选中项
        /// </summary>
        IGroupItem[] SelectedItems { get; }
    }
}
