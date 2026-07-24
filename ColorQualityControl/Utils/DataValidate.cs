using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;//正则表达式

namespace ColorQualityControl.Utils
{
    /// <summary>
    /// 通用验证类
    /// </summary>
    public class DataValidate
    {
        /// <summary>
        /// 验证正整数
        /// </summary>     
        public static bool IsInteger(string txt)
        {
            Regex objReg = new Regex(@"^[1-9]\d*$");
            return objReg.IsMatch(txt);
        }
        public static bool IsZeroInteger(string txt)
        {
            Regex objReg = new Regex(@"^[0-9]\d*$");
            return objReg.IsMatch(txt);
        }
        /// <summary>
        /// 验证是否是Email
        /// </summary>     
        public static bool IsEmail(string txt)
        {
            Regex objReg = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*");
            return objReg.IsMatch(txt);
        }
        /// <summary>
        /// 验证身份证
        /// </summary>     
        public static bool IsIdentityCard(string txt)
        {
            Regex objReg = new Regex(@"^(\d{15}$|^\d{18}$|^\d{17}(\d|X|x))$");
            return objReg.IsMatch(txt);
        }

        //其他的验证方法在下面继续添加
        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        public static bool IsNumber(string txt)
        {
            double value;
            var result= double.TryParse(txt, out value); 
            return result;
        }
        public static bool isNumberWithPoint(string txt)
        {
            //^-{0,1}[0-9]+(.[0-9]+){0,1}$
            Regex objReg = new Regex(@"^(([0-9]+\.[0-9]*[1-9][0-9]*)|([0-9]*[1-9][0-9]*\.[0-9]+)|([0-9]*[1-9][0-9]*))$");
            var result = objReg.IsMatch(txt);
            return result;
        }
        /// <summary>
        /// 汉字验证
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        public static bool IsChineseName(string txt)
        {
            Regex objReg = new Regex(@"^[\u4e00-\u9fa5]{0,}$");
            return objReg.IsMatch(txt);
        }
        /// <summary>
        /// 英文字母验证
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        public static bool IsEnglishName(string txt)
        {
            Regex objReg = new Regex(@"^[A-Za-z]+$");
            return objReg.IsMatch(txt);
        }
        public static bool ContainsChinese(string str)
        {
            return Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
        }
        //±(3%H+1)
        public static bool ErrorRangeStringFormat(string str)
        {
            try
            {
                var result1 = str.Contains("±") &&
                                str.Contains("(") && (str.IndexOf("(") - str.IndexOf("±") == 1) &&
                                str.Contains("%") &&
                                str.Contains("H") && (str.IndexOf("H") - str.IndexOf("%") == 1) &&
                                str.Contains("+") && (str.IndexOf("+") - str.IndexOf("H") == 1) &&
                                str.Contains(")") && str.Last().ToString() == ")";

                //获取第一个值（取“(”--"%"中间数值）
                var index1 = str.IndexOf('(');
                var index2 = str.IndexOf("%");
                var tempChars1 = str.Skip(index1 + 1).Take(index2 - index1 - 1).ToArray();
                string tempStr1 = new string(tempChars1);

                //获取第二个值（取“+”--")"中间数值）
                var index3 = str.IndexOf("+");
                var index4 = str.IndexOf(')');
                var tempChars2 = str.Skip(index3 + 1).Take(index4 - index3 - 1).ToArray();
                string tempStr2 = new string(tempChars2);
                bool result2 = IsNumber(tempStr1) && IsNumber(tempStr2);
                return result2 && result1;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
           
         
        }
    }
}
