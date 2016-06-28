using System;
using System.Text.RegularExpressions;

namespace SourceLinesSocket
{
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
        int SendInterval { get; set; }
        int RunningSeconds { get; set; }
        int MessagesPerConnection { get; set; }
        bool QuitIfExceededAny { get; set; }
        int MaxConnectTimes { get; set; }

        int PauseSecondsAtDrop { get; set; }
    }

    [Serializable]
    public static class Extension
    {
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
