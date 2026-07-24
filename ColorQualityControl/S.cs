using ColorQualityControl.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ThreeNH.ColorQualityControl
{
    static class S
    {
        public static string _(string key)
        {
            //#if DEBUG
            //            return (Application.Current as App).LanguateResourceDictionary[key] as string;
            //#else
#if true
            if (Application.Current.Resources.Contains(key))
            {
                var s = Application.Current.Resources[key] as string;
                if (s != null)
                {
                    return s;
                }
            }
                return key;
#else
            var app = (Application.Current as App);
            if (LocalUtil.LanguateResourceDictionary != null && LocalUtil.LanguateResourceDictionary.Contains(key))
            {
                var value = LocalUtil.LanguateResourceDictionary[key];
                return value == null ? key : value as string;
            }
            return key;

        }
#endif
            }
        }
    }
