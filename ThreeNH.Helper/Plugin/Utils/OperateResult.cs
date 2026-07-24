using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.Helper.Plugin.Utils
{
    public  class OperateResult
    {
        private bool _isSuccess;

        /// <summary>
        /// 操作返回信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 操作错误代码
        /// </summary>
        public int ErrorCode { get; set; }

        public OperateResult()
        {
        }

        /// <summary>
        /// 使用指定的消息实例化一个默认的结果对象
        /// </summary>
        /// <param name="msg">错误消息</param>
        public OperateResult(string msg)
        {
            Message = msg;
        }

        /// <summary>
        /// 使用错误代码，消息文本来实例化对象
        /// </summary>
        /// <param name="err">错误代码</param>
        /// <param name="msg">错误消息</param>
        public OperateResult(int err, string msg)
        {
            ErrorCode = err;
            Message = msg;
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set
            {
                _isSuccess = value;
                if(_isSuccess)
                {
                    if(string.IsNullOrEmpty(Message))
                    {
                        Message = "操作成功";
                    }
                }
                else
                {
                    if(string.IsNullOrEmpty(Message))
                    {
                        Message = "操作失败";
                    }
                }
            }
        }

        public bool IsFailed => !IsSuccess;

        /// <summary>
        ///  结果返回
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("错误代码：{0}   错误信息：{1}", ErrorCode, Message);
        }

        public static string ConvertException(Exception ex)
        {
            return ex.Message + ex.InnerException + Environment.NewLine + ex.StackTrace;
        }
      
        public static string ConvertExMessage(Exception ex)
        {
            return ex.Message;
        }

        /// <summary>
        /// 创建失败的信息返回对象
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="errorCode"></param>
        /// <returns></returns>
        public static OperateResult CreateFailedResult(string msg= "操作失败", int errorCode=1)
        {
            return new OperateResult()
            {
                IsSuccess = false,
                ErrorCode = errorCode,
                Message = msg,
            };
        }

        /// <summary>
        /// 创建一个带成功信息的成功对象
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static OperateResult CreateSuccessResult(string msg= "操作成功")
        {
            return new OperateResult()
            {
                IsSuccess = true,
                ErrorCode = 0,
                Message = msg,
            };
        }

        /// <summary>
        /// 创建并返回一个失败的结果对象，该对象复制另一个结果对象的错误信息
        /// </summary>
        /// <typeparam name="T">目标数据类型</typeparam>
        /// <param name="result">之前的结果对象</param>
        /// <returns>带默认泛型对象的失败结果类</returns>
        public static OperateResult<T> CreateFailedResult<T>(OperateResult result)
        {
            return new OperateResult<T>()
            {
                ErrorCode = result.ErrorCode,
                Message = result.Message,
            };
        }

        /// <summary>
        /// 创建并返回一个成功的结果对象，并带有一个参数对象
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="value">类型的值对象</param>
        /// <returns>成功的结果对象</returns>
        public static OperateResult<T> CreateSuccessResult<T>(T value, string msg="操作成功")
        {
            return new OperateResult<T>()
            {
                IsSuccess = true,
                ErrorCode = 0,
                Content = value,
                Message = msg
            };
        }

        /// <summary>
        /// 失败的返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static OperateResult<T> CreateFailedResult<T>(T value, string msg="操作失败")
        {
            return new OperateResult<T>()
            {
                IsSuccess = false,
                ErrorCode = 0,
                Message = msg,
                Content = value
            };
        }

    }

    /// <summary>
    /// 操作结果的泛型类，允许带一个用户自定义的泛型对象，推荐使用这个类
    /// </summary>
    /// <typeparam name="T">泛型类</typeparam>
    public class OperateResult<T> : OperateResult
    {
        #region Constructor

        /// <summary>
        /// 实例化一个默认的结果对象
        /// </summary>
        public OperateResult()
            : base()
        {
        }

        /// <summary>
        /// 使用指定的消息实例化一个默认的结果对象
        /// </summary>
        /// <param name="msg">错误消息</param>
        public OperateResult(string msg)
            : base(msg)
        {

        }

        /// <summary>
        /// 使用错误代码，消息文本来实例化对象
        /// </summary>
        /// <param name="err">错误代码</param>
        /// <param name="msg">错误消息</param>
        public OperateResult(int err, string msg)
            : base(err, msg)
        {

        }

        #endregion

        /// <summary>
        /// 用户自定义的泛型数据
        /// </summary>
        public T Content { get; set; }
    }
}
