using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeNH.ColorSpecimen;
using ThreeNH.Instruments.Measurement;

namespace ThreeNH.ColorimeterInterface
{
    /// <summary>
    /// 仪器测量接口
    /// </summary>
    public interface IMeasurement : IMeasurementDevice<Specimen>
    {
       
    }

    public class MeasurementNotificationArgs : EventArgs
    {
        public MeasurementNotificationArgs(Specimen result)
        {
            Result = result;
        }

        public Specimen Result { get; }
    }

    /// <summary>
    /// 测量通知接口，用于支持仪器端按键测量后测量结果自动上传的仪器
    /// </summary>
    public interface IMeasurementNotification
    {
        /// <summary>
        /// 接收到仪器端的测量通知事件
        /// </summary>
        event EventHandler<MeasurementNotificationArgs> MeasurementReceived;
    }
}
