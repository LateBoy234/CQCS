using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace ThreeNH.Controls
{
    public class Loading
    {
        private BackgroundWorker _backgroundWorker;
        private LoadingForm _loadingForm;
        private BackgroundWorkerEventArgs _args;

        public event EventHandler<ProgressChangedEventArgs> BackgroundWorkerProgressChanged;
        public Loading(string msg = "", string caption = "")
        {
            _loadingForm = new LoadingForm();
            _loadingForm.CancelDelegate += OnCancel;
            if (!string.IsNullOrEmpty(msg))
                _loadingForm.Message = msg;
            if (!string.IsNullOrEmpty(caption))
                _loadingForm.Caption = caption;
            _args = new BackgroundWorkerEventArgs();
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += _backgroundWorker_DoWork;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.WorkerReportsProgress = false;
            _backgroundWorker.RunWorkerCompleted += _backgroundWorker_RunWorkerCompleted;
            _backgroundWorker.ProgressChanged += _backgroundWorker_ProgressChanged;
        }
        public bool ShowingProgress
        {
            get => _loadingForm.IsProgressBarVisible == Visibility.Visible;
            set => _loadingForm.IsProgressBarVisible = value ? Visibility.Visible : Visibility.Collapsed;
        }

        public bool CanBeCanceled
        {
            get => _loadingForm.IsCancelButtonVisible == Visibility.Visible;
            set => _loadingForm.IsCancelButtonVisible = value ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnCancel()
        {
            _args.IsCancelled = true;
            _backgroundWorker.CancelAsync();
            _args.IsCancelled = true;
        }


        public Action<Action<object>, Action<object>, BackgroundWorkerEventArgs> BackgroundWork { get; set; }

        /// <summary>
        /// 后台任务执行完毕后事件
        /// </summary>
        public event EventHandler<BackgroundWorkerEventArgs> BackgroundWorkerCompleted;

        public void Start()
        {
            try
            {
                _backgroundWorker.RunWorkerAsync();
                _loadingForm.ShowDialog();
            }
            catch (Exception ex)
            {
                _args.Result = ex;
            }
        }

        private void _backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BackgroundWorkerProgressChanged?.Invoke(null, e);
        }

        private void _backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_loadingForm.IsVisible)
                _loadingForm.Close();
            BackgroundWorkerCompleted?.Invoke(this, _args);

            // 取消订阅事件，防止内存泄漏
            _backgroundWorker.RunWorkerCompleted -= _backgroundWorker_RunWorkerCompleted;
            _backgroundWorker.ProgressChanged -= _backgroundWorker_ProgressChanged;
        }

        private void _backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                BackgroundWork?.Invoke(ReportProgress, SetResult, _args);
            }
            catch (Exception ex)
            {
                _args.Result = ex;
            }
        }

        private void SetResult(object obj)
        {
            _args.Result = obj;
        }

        private void ReportProgress(object value)
        {
            if (value is string message)
                _loadingForm.Message = message;
            else if (value is int progress)
                _loadingForm.Progress = progress;
        }
    }
}
