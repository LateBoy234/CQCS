using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.SQCX.Infrastructure.Instruments.YL30Comm.Result
{
    public class RequestResult
    {
        public RequestResult(ResultType type)
        {
          this.ResultType = type; 
        }
        public RequestResult(ResultType type,string msg)
        {
            this.ResultType = type;
            this.Msg = msg;
        }
        public RequestResult(ResultType type, string msg,object obj)
        {
            this.ResultType = type;
            this.Msg = msg;
            this.Obj = obj;
        }
     public ResultType ResultType { get; set; }
      public string Msg { get; set; }
      public object Obj { get; set; }
    }

    public class RequestResult<T>
    {
        public RequestResult(ResultType type)
        {
            this.ResultType = type;
        }
        public RequestResult(ResultType type, string msg)
        {
            this.ResultType = type;
            this.Msg = msg;
        }
        public RequestResult(ResultType type, string msg, T obj)
        {
            this.ResultType = type;
            this.Msg = msg;
            this.Obj = obj;
        }
        public ResultType ResultType { get; set; }
        public string Msg { get; set; }
        public T Obj { get; set; }
    }
}
