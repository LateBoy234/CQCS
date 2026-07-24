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
        public static void ShowToastInfo(string msg)
        {
            _notifier.ShowInformation(msg);
        }
        public static void DisposeToastNotificationsObject()
        {
            _notifier.Dispose();
        }
    }
}
