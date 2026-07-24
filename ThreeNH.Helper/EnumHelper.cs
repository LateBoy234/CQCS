using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace ThreeNH.Helper
{
    public struct ValueDescription
    {
        public Enum Value { get; set; }
        public string Description { get; set; }
    }

    public static class EnumHelper
    {
        static IDictionary _translation;

        public static void SetTranslation(IDictionary translation)
        {
            _translation = translation;
        }
        
        static string _(string text)
        {
#if true
            if (Application.Current.Resources.Contains(text))
            {
                var s = Application.Current.Resources[text] as string;
                if (s != null)
                {
                    return s;
                }
            }
            return text;
#else
            if (_translation != null && _translation.Contains(text) && _translation[text] != null && _translation[text] is string)
            {
                return _translation[text] as string;
            }
            return text;
#endif
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
                    return _((nAttributes.First() as DescriptionAttribute).Description);
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
                            list.Add(_((nAttributes.First() as DescriptionAttribute).Description));
                        else
                            list.Add(enumFlag.ToString());
                    }
                }

                if (list.Count > 0)
                {
                    return string.Join(", ", list);
                }
            }

            return _(eValue.ToString());
        }

        public static IEnumerable<ValueDescription> GetAllValuesAndDescriptions(Type t, Predicate<Enum> predicate = null)
        {
            if (!t.IsEnum)
                throw new ArgumentException("t must be an enum type");

            if (predicate != null)
            {
                return from a in Enum.GetValues(t).Cast<Enum>()
                       where predicate.Invoke(a)
                       select new ValueDescription() { Value = a, Description = a.Description() };
            }
            return from a in Enum.GetValues(t).Cast<Enum>()
                   select new ValueDescription() { Value = a, Description = a.Description() };
        }

        /// <summary>
        /// 枚举 → 描述 + 名称 + 数字 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
       

        /// <summary>
        /// 把一个枚举（Enum）转换成「描述文本 + 枚举值」的列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<string, Enum>> GetEnumValuesWithDescriptions<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<Enum>().Select(e => new KeyValuePair<string, Enum>(e.Description(), e));
        }
    }
}
