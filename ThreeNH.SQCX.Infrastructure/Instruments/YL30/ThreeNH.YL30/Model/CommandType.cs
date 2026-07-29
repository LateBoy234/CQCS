﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30.Model
{
    public enum CommandType
    {
        ReadingCommand,
        WritingCommand
    }

    #region socket连接的实体类
    public enum Cmd
    {
        INF, TMP, TIM, MEA, MCR, MCS, CAL, CVR, WBR, WBS, GET, SET, UVC, FSR, DC, STEP
    }

   
    #endregion
}
