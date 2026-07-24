using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ThreeNH.Helper.Plugin.Interfaces;

namespace ThreeNH.Helper.Plugin.Services
{
    public  class LoadingService : ILoadingDisplay
    {
        private LoadingWindow _window;
        private readonly SynchronizationContext _uiContext;
        private bool _isDisposed = false;

        #region 属性实现

        public string Caption
        {
            get => Invoke(() => _window?.Caption);
            set => Invoke(() => { if (_window != null) _window.Caption = value; });
        }

        public string Message
        {
            get => Invoke(() => _window?.Message);
            set => Invoke(() => { if (_window != null) _window.Message = value; });
        }

        public int Progress
        {
            get => Invoke(() => _window?.Progress ?? 0);
            set => Invoke(() => { if (_window != null) _window.Progress = value; });
        }

        public bool CanCancel
        {
            get => Invoke(() => (_window?.IsCancelButtonVisible ?? Visibility.Collapsed) == Visibility.Visible);
            set => Invoke(() =>
            {
                if (_window != null)
                    _window.IsCancelButtonVisible = value ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        public string CancelContent
        {
            get => Invoke(() => _window?.CancelContent);
            set => Invoke(() => { if (_window != null) _window.CancelContent = value; });
        }

        public event Action Cancelled;

        #endregion

        #region 构造函数

       
        public LoadingService()
        {
            // 捕获当前同步上下文（通常是UI线程）
            _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        #endregion

        #region 公共方法

        public void Show()
        {
            Invoke(() =>
            {
                if (_window != null) return;

                _window = new LoadingWindow();
                _window.CancelDelegate += OnCancelled;
                _window.Closed += (s, e) => _window = null;
                _window.Show();
            });
        }

        public void ShowDialog()
        {
            Invoke(() =>
            {
                if (_window != null) return;

                _window = new LoadingWindow();
                _window.CancelDelegate += OnCancelled;
                _window.Closed += (s, e) => _window = null;
                _window.ShowDialog();
            });
        }

        public void Close()
        {
            Invoke(() =>
            {
                _window?.Close();
                _window = null;
            });
        }

        public void UpdateProgress(int progress, string message = null)
        {
            Invoke(() =>
            {
                if (_window == null) return;

                _window.Progress = progress;
                if (message != null)
                    _window.Message = message;
            });
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            Close();
            _isDisposed = true;

            GC.SuppressFinalize(this);
        }

        #endregion

        #region 私有方法

        private void OnCancelled()
        {
            Cancelled?.Invoke();
        }

        /// <summary>
        /// 确保在UI线程执行操作
        /// </summary>
        private void Invoke(Action action)
        {
            if (_uiContext == SynchronizationContext.Current)
                action();
            else
                _uiContext.Send(_ => action(), null);
        }

        /// <summary>
        /// 确保在UI线程执行操作（带返回值）
        /// </summary>
        private T Invoke<T>(Func<T> func)
        {
            T result = default;
            if (_uiContext == SynchronizationContext.Current)
                result = func();
            else
                _uiContext.Send(_ => result = func(), null);
            return result;
        }

        #endregion
    }
}
