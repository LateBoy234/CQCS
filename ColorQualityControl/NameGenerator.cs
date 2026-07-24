using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ThreeNH.ColorSpecimen;

namespace ThreeNH.ColorQualityControl
{
    public class NameGenerator
    {
        /// <summary>
        /// 格式化给定的变量
        /// </summary>
        /// <param name="variable">变量名</param>
        /// <param name="format">格式化字符串</param>
        /// <param name="parameter">附加的参数</param>
        /// <returns>返回格式化后的字符串；如果不存在该变量，返回null</returns>
        public delegate string VariableEvaluatorDelegate(string variable, string format, object parameter=null);

        private const string VariablePattern = @"{(?<variable>[\w\d]+)(:(?<format>.*?))?}";
        private Regex variableRegex = new Regex(VariablePattern);

        public NameGenerator()
        {
            Variables = new Dictionary<string, object>();
            VariableEvaluator = DefaultVariableEvaluator;
        }

        public NameGenerator(VariableEvaluatorDelegate evaluator)
        {
            Variables = new Dictionary<string, object>();
            VariableEvaluator = evaluator;
        }

        /// <summary>
        /// 替换并格式化变量
        /// </summary>
        public VariableEvaluatorDelegate VariableEvaluator;


        /// <summary>
        /// 验证规则是否有效
        /// </summary>
        /// <param name="rule">要验证的规则</param>
        /// <returns>返回验证结果</returns>
        public static bool VerifyRule(string rule)
        {
            // 规则不能为空
            if (string.IsNullOrWhiteSpace(rule))
                return false;
            // 规则中不能包含空格以外的空字符
            string tmp = rule.Replace(" ", "");
            Regex regex = new Regex(@"\s+");
            return !regex.IsMatch(tmp);
        }
        /// <summary>
        /// 命名规则
        /// </summary>
        public string Rule { get; set; }
        /// <summary>
        /// 变量
        /// </summary>
        public Dictionary<string, object> Variables { get; private set; }

        /// <summary>
        /// 新名称
        /// </summary>
        public string NewName
        {
            get
            {
                return variableRegex.Replace(Rule, m => ReplaceVariable(m));
            }
        }

        public string GenerateName(object parameter)
        {
            return variableRegex.Replace(Rule, m => ReplaceVariable(m, parameter));
        }

        /// <summary>
        /// 使用变量值替换匹配的规则
        /// </summary>
        /// <param name="match">匹配结果</param>
        /// <returns>返回替换后的值</returns>
        private string ReplaceVariable(Match match, object parameter = null)
        {
            string variable = match.Groups["variable"].Value;
            string format = match.Groups["format"].Value;
            if (VariableEvaluator != null)
            {
                return VariableEvaluator(variable, format, parameter);
            }
            return match.Value;
        }


        /// <summary>
        /// 替换并根据格式字符串格式化变量，如果不存在相应的变量返回null
        /// </summary>
        /// <param name="variable">变量名</param>
        /// <param name="format">格式字符串</param>
        /// <returns>返回变量的值</returns>
        protected virtual string DefaultVariableEvaluator(string variable, string format, object parameter)
        {
            if (Variables.ContainsKey(variable))
            {
                return Variables[variable].ToString();
            }
            return null;
        }
    }
}
