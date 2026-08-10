using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreeNH.CommProtocol
{

    public enum OperationCode
    {
        InvalidOperation,
        WriteFlash,
        ReadFlash,
        WriteResource,
        WriteFireware,
        WriteFile,
        ReadFile,
        ListFileNames,
        DeleteFile,
        WriteLogo,
    }
}
