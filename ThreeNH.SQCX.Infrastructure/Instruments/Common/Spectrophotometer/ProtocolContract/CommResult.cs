﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.SQCX.Infrastructure.Instruments.Spectrophotometer.ProtocolContract
{
    public class CommResult
    {
        internal CommResult(string ackLine)
        {
            if (ackLine.StartsWith("OK"))
            {
                IsOk = true;
                int index = ackLine.IndexOf(' ');
                if (index > 0)
                {
                    Json = ackLine.Substring(index + 1);
                }
            }
            else if (ackLine.StartsWith("ERR"))
            {
                if (int.TryParse(ackLine.Substring(3), out int code))
                {
                    ErrorCode = code;
                }
                IsOk = false;
            }
            else
            {
                Trace.TraceError("Unknown ack: {0}", ackLine);
            }
        }

        public bool IsOk { get; set; } = false;

        /// <summary>
        /// 错误码
        /// </summary>
        public int ErrorCode { get; set; } = 0;

        /// <summary>
        /// 执行结果的数据
        /// </summary>
        public string Json { get; set; } = null;
    }

    public class NotificationEventArgs : EventArgs
    {
        public string Json { get; set; }

        public NotificationEventArgs(string json)
        {
            Json = json;
        }
    }
}
