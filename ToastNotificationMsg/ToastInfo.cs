using System;
using ToastNotifications;
using ToastNotifications.Messages;
using ToastNotifications.Position;
using ToastNotifications.Lifetime;
using System.Windows;

namespace ToastNotificationMsg
{
    static public class ToastInfo
    {
        static Notifier _notifier = new Notifier(
    cfg => {
    cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.TopRight,
                offsetX: 10,
                offsetY: 10
                    );
    cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(3),
                maximumNotificationCount: MaximumNotificationCount.FromCount(5)
                    );
    cfg.Dispatcher = Application.Current.Dispatcher;
}
    );

        /// <summary>
        /// 蓝色信息提示
        /// </summary>
        /// <param name="msg"></param>
        public static void ShowToastInfo(string msg)
        {
            _notifier.ShowInformation(msg);
        }
        /// <summary>
        /// 错误红色提示
        /// </summary>
        /// <param name="msg"></param>
        public static void ShowToastError(string msg)
        {
            _notifier.ShowError(msg);
        }
        /// <summary>
        /// 成功绿色提示
        /// </summary>
        /// <param name="msg"></param>
        public static void ShowToastSuccess(string msg)
        {
            _notifier.ShowSuccess(msg);
        }

        /// <summary>
        /// 警告黄色提示
        /// </summary>
        /// <param name="msg"></param>
        public static void ShowToastWorning(string msg)
        {
            _notifier.ShowWarning(msg);
        }

        public static void DisposeToastNotificationsObject()
        {
            _notifier.Dispose();
        }
    }
}
