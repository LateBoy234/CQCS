using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeNH.CommDevice;

namespace ThreeNH.Spectrophotometer.ProtocolContract
{
    public interface IJsonCommProtocol : IDisposable
    {
        event EventHandler<NotificationEventArgs> NotificationReceived;

        ICommDevice CommDevice { get; set; }
        bool IsOpen { get; }

        CommResult send(string cmd, string param = null, int timeout = 5000);

        void Close();

        void Open(string deviceName = null);
    }
}
