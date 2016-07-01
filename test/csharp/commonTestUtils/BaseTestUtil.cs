using System;
using System.Text.RegularExpressions;

namespace CommonTestUtils
{
    //public class BaseTestUtil<ClassName, TimeFormatString = "yyyy-MM-dd HH:mm:ss", IsUTC = false>

    [Serializable]
    public class BaseTestUtil<ClassName>
    {
        public const string DateFormat = "yyyy-MM-dd";
        public const string TimeFormat = "HH:mm:ss";
        public const string DateTimeFormat = DateFormat + " " + TimeFormat;
        public const string MilliTimeFormat = TimeFormat + ".fff";
        public const string MicroTimeFormat = MilliTimeFormat + "fff";
        public const string MilliDateTimeFormat = DateTimeFormat + ".fff";
        public const string MicroDateTimeFormat = MilliDateTimeFormat + "fff";

        public static String Now { get { return DateTime.Now.ToString(DateTimeFormat); } }

        public static String NowMilli { get { return DateTime.Now.ToString(MilliDateTimeFormat); } }

        public static String NowMicro { get { return DateTime.Now.ToString(MicroDateTimeFormat); } }

        public static String UtcNow { get { return DateTime.UtcNow.ToString(DateTimeFormat); } }

        public static String UtcNowMilli { get { return DateTime.UtcNow.ToString(MilliDateTimeFormat); } }

        public static String UtcNowMicro { get { return DateTime.UtcNow.ToString(MicroDateTimeFormat); } }

        public static void Log(String format, params object[] args)
        {
            Console.WriteLine("{0} {1}", GetHeader(), string.Format(format, args));
        }

        public static void Log(String format, object arg0)
        {
            Console.WriteLine("{0} {1}", GetHeader(), string.Format(format, arg0));
        }

        public static void Log(IFormatProvider provider, String format, params object[] args)
        {
            Console.WriteLine("{0} {1}", GetHeader(), string.Format(provider, format, args));
        }

        public static void Log(String format, object arg0, object arg1)
        {
            Console.WriteLine("{0} {1}", GetHeader(), string.Format(format, arg0, arg1));
        }

        public static void Log(String format, object arg0, object arg1, object arg2)
        {
            Console.WriteLine("{0} {1}", GetHeader(), string.Format(format, arg0, arg1, arg2));
        }

        #region for overloading
        protected static string GetTypeName()
        {
            return typeof(ClassName).Name;
        }

        protected static DateTime GetTimeNow()
        {
            return DateTime.Now;
        }

        protected static string GetTimeFormat()
        {
            return MilliDateTimeFormat;
        }

        protected static string GetHeader()
        {
            return string.Format("{0} {1}", GetTimeNow().ToString(GetTimeFormat()), GetTypeName());
        }

        #endregion
    }

    [Serializable]
    public class BaseTestUtilLongClass<ClassName> : BaseTestUtil<ClassName>
    {
        protected new static string GetTypeName()
        {
            return typeof(ClassName).ToString();
        }
    }

    [Serializable]
    public class BaseTestUtilUtc<ClassName> : BaseTestUtil<ClassName>
    {
        protected new static DateTime GetTimeNow()
        {
            return DateTime.UtcNow;
        }
    }

    [Serializable]
    public class BaseTestUtilLongClassUtc<ClassName> : BaseTestUtilLongClass<ClassName>
    {
        protected new static DateTime GetTimeNow()
        {
            return DateTime.UtcNow;
        }
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
