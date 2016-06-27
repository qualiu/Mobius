﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace testKeyValueStream
{
    public static class TestUtils
    {
        public const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
        public const string MilliTimeFormat = TimeFormat + ".fff";
        public const string MicroTimeFormat = MilliTimeFormat + "fff";

        public static string Now { get { return DateTime.Now.ToString(TimeFormat); } }

        public static string NowMilli { get { return DateTime.Now.ToString(MilliTimeFormat); } }

        public static string NowMicro { get { return DateTime.Now.ToString(MicroTimeFormat); } }

        public static string ArrayToText<T>(T[] array, int takeMaxElementCount = 9)
        {
            if (array == null)
            {
                return "[] = null";
            }
            else if (array.Length == 0)
            {
                return "[0] = " + array;
            }
            else if (array.Length <= takeMaxElementCount)
            {
                return "[" + array.Length + "] = " + string.Join(", ", array);
            }
            else
            {
                return "[" + array.Length + "] = " + string.Join(", ", array.Take(takeMaxElementCount)) + ", ... , " + array.Last();
            }
        }

        public static long GetFirstElementValue<TData>(TData data)
        {
            var en = data as IEnumerable;
            if (en != null)
            {
                var it = en.GetEnumerator();
                it.MoveNext();
                return Convert.ToInt64(it.Current);
            }
            else
            {
                return Convert.ToInt64(data);
            }

            throw new ArgumentException("not found type for " + data);
        }

        public static string GetValueText(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is int)
            {
                return ((int)value).ToString();
            }
            else if (value is int[])
            {
                return TestUtils.ArrayToText((int[])value);
            }
            else
            {
                return value.ToString();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="ArgType"></typeparam>
        /// <param name="index">start from -1</param>
        /// <param name="args"></param>
        /// <param name="argName"></param>
        /// <param name="defaultValue"></param>
        /// <param name="canBeOmitted">omitted, not in args</param>
        /// <returns></returns>
        public static ArgType GetArgValue<ArgType>(ref int index, string[] args, string argName, ArgType defaultValue, bool canBeOmitted = true, string className = "")
        {
            index++;
            var header = string.IsNullOrEmpty(className) ? string.Empty : className + " ";
            if (args.Length > index)
            {
                Console.WriteLine("{0}args[{1}] : {2} = {3}", header, index, argName, args[index]);
                var argValue = args[index];
                if (defaultValue is bool)
                {
                    argValue = Regex.IsMatch(args[index], "1|true", RegexOptions.IgnoreCase).ToString();
                }

                return (ArgType)TypeDescriptor.GetConverter(typeof(ArgType)).ConvertFromString(argValue);
            }
            else if (canBeOmitted)
            {
                Console.WriteLine("{0}args-{1} : {2} = {3}", header, index, argName, defaultValue);
                return defaultValue;
            }
            else
            {
                throw new ArgumentException(string.Format("{0}must set {1} at arg[{2}]", header, argName, index + 1), argName);
            }
        }
        
        public static ArgType GetArgValue<ArgType>(string classType, ref int index, string[] args, string argName, ArgType defaultValue, bool canBeOmitted = true)
        {
            return GetArgValue(ref index, args, argName, defaultValue, canBeOmitted, classType);
        }

        public static IPAddress GetHost(bool print = false)
        {
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            var ipList = new List<IPAddress>();
            foreach (IPAddress ipa in ips)
            {
                if (ipa.AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }

                if (print)
                {
                    Console.WriteLine("ip = {0}, AddressFamily = {1}", ipa, ipa.AddressFamily);
                }

                var ip = ipa.ToString();
                if (!ip.StartsWith("10.0.2.") && !ip.StartsWith("192.168."))
                {
                    return ipa;
                }
            }

            return IPAddress.Parse("127.0.0.1");
        }
    }
}
