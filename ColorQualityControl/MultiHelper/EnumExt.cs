using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ThreeNH.ColorQualityControl.MultiHelper
{
    public static class EnumExt
    {
        public static List<Tuple<string, string, int>> GetAllDescriptions<T>() where T : Enum
        {
            var result = new List<Tuple<string, string, int>>();
            Type enumType = typeof(T);
            foreach (Enum value in Enum.GetValues(enumType))
            {
                FieldInfo field = enumType.GetField(value.ToString());
                DescriptionAttribute attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))
                                                 as DescriptionAttribute;
                string desc = attribute?.Description ?? value.ToString();
                result.Add(Tuple.Create(desc, value.ToString(), Convert.ToInt32(value)));
            }
            return result;
        }

        public static string Description(this Enum eValue)
        {
            Type type = eValue.GetType();
            var flagsAttributs = type.GetCustomAttributes(typeof(FlagsAttribute), false);
            if (Convert.ToInt32(eValue) == 0 || !flagsAttributs.Any())
            {
                var nAttributes = type.GetField(eValue.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (nAttributes.Any())
                    return (nAttributes.First() as DescriptionAttribute).Description;
            }
            else
            {
                List<string> list = new List<string>();
                foreach (int flag in Enum.GetValues(type))
                {
                    if (flag == 0)
                        continue;

                    Enum enumFlag = (Enum)Enum.ToObject(type, flag);
                    if (eValue.HasFlag(enumFlag))
                    {
                        var nAttributes = type.GetField(enumFlag.ToString())
                            .GetCustomAttributes(typeof(DescriptionAttribute), false);

                        if (nAttributes.Any())
                            list.Add((nAttributes.First() as DescriptionAttribute).Description);
                        else
                            list.Add(enumFlag.ToString());
                    }
                }
                if (list.Count > 0)
                {
                    return string.Join(", ", list);
                }
            }
            return eValue.ToString();
        }

        public static T GetEnumValueFromDescription<T>(string description) where T : Enum
        {

            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))
                    is DescriptionAttribute attribute)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            throw new ArgumentException($"未找到匹配 {description} 的枚举值", nameof(description));
        }

    }
}
