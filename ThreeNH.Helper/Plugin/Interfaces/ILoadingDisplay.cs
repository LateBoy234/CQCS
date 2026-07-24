using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.Helper.Plugin.Interfaces
{
    public interface ILoadingDisplay : IDisposable
    {
        /// <summary>
        /// 窗口标题/标题栏文本
        /// </summary>
        string Caption { get; set; }

        /// <summary>
        /// 当前状态描述信息
        /// </summary>
        string Message { get; set; }

        /// <summary>
        /// 当前进度值 (0-100)
        /// </summary>
        int Progress { get; set; }

       

        /// <summary>
        /// 是否显示取消按钮
        /// </summary>
        bool CanCancel { get; set; }

        /// <summary>
        /// 取消按钮文本
        /// </summary>
        string CancelContent { get; set; }

        /// <summary>
        /// 用户点击取消时触发的事件
        /// </summary>
        event Action Cancelled;

        /// <summary>
        /// 显示为非模态窗口
        /// </summary>
        void Show();

        /// <summary>
        /// 显示为模态对话框
        /// </summary>
        void ShowDialog();

        /// <summary>
        /// 关闭加载窗口
        /// </summary>
        void Close();

        /// <summary>
        /// 更新进度信息（线程安全）
        /// </summary>
        void UpdateProgress(int progress, string message = null);
    }
}
