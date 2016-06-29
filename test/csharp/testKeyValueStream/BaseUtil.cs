using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace testKeyValueStream
{
    [Serializable]
    public class BaseUtil<ClassName>
    {
        public const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
        public const string MilliTimeFormat = TimeFormat + ".fff";
        public const string MicroTimeFormat = MilliTimeFormat + "fff";

        public static void Log(string message)
        {
            Console.WriteLine("{0} {1} : {2}", DateTime.Now.ToString(MilliTimeFormat), typeof(ClassName).Name, message);
        }
    }

    public interface IArgOptions
    {
        string Host { get; set; }
        int Port { get; set; }
        int BatchSeconds { get; set; }
        int WindowSeconds { get; set; }
        int SlideSeconds { get; set; }
        int RunningSeconds { get; set; }
        int TestTimes { get; set; }
        string CheckPointDirectory { get; set; } // = Path.Combine(Path.GetTempPath(), "checkDir")
        bool NeedDeleteCheckPointDirectory { get; set; }
        string MethodName { get; set; }
        bool IsArrayValue { get; set; }
        bool IsUnevenArray { get; set; }
        long ElementCount { get; set; }
        string SaveTxtDirectory { get; set; }
        bool CheckArray { get; set; }
        Int64 ValidateCount { get; set; }
    }

    [Serializable]
    public static class Extensions
    {
        public static bool IsReduceByKey(this IArgOptions options)
        {
            return options.MethodName.Equals("reduceByKey", StringComparison.CurrentCultureIgnoreCase);
        }

        public static void OutArgs<TClass>(this TClass options, Action<string, object> OutNameValueFunc = null, string regexExcludeNames = "^(help)$", bool regexIgnoreCase = true)
        {
            var tp = options.GetType();
            var properties = tp.GetProperties();

            Action<string, object> OutNameValue = (name, value) =>
            {
                Console.WriteLine("{0} = {1}", name, value);
            };

            if (OutNameValueFunc == null)
            {
                OutNameValueFunc = OutNameValue;
            }

            var regexExclude = string.IsNullOrWhiteSpace(regexExcludeNames) ? null : new Regex(regexExcludeNames, regexIgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

            foreach (var property in properties)
            {
                if (null != regexExclude && regexExclude.IsMatch(property.Name))
                {
                    continue;
                }
                var pv = property.GetValue(options);
                OutNameValueFunc(property.Name, pv);
            }
        }
    }
}
