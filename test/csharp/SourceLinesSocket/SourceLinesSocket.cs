using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SourceLinesSocket
{
    class SourceLinesSocket : BaseUtil<SourceLinesSocket>
    {
        private static Socket ServerSocket;
        private static IPAddress HostAddress;

        private static volatile bool IsTimeout = false;

        private static Int64 TotalSentMessages = 0;

        private static int ConnectedTimes = 0;


        static void Main(string[] args)
        {
            var exeName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);

            var parseOK = false;
            //var Options = ParserByCommandLine.Parse(args, out parseOK);
            //var Options = ParserByFluent.Parse(args, out parseOK);
            var options = ParserByPowerArgs.Parse(args, out parseOK);

            if (!parseOK)
            {
                return;
            }

            var runningDuration = options.RunningSeconds <= 0 ? TimeSpan.MaxValue : TimeSpan.FromSeconds(options.RunningSeconds);

            HostAddress = !string.IsNullOrWhiteSpace(options.Host) ? IPAddress.Parse(options.Host) : GetHost();
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.Bind(new IPEndPoint(HostAddress, options.Port));
            ServerSocket.Listen(10);

            var startTime = DateTime.Now;
            var thread = new Thread(() => { ListenToClient(startTime, options, runningDuration); });
            thread.IsBackground = true; // Options.RunningSeconds > 0;
            thread.Start();

            if (options.RunningSeconds < 1)
            {
                return;
            }

            var process = Process.GetCurrentProcess();
            var stopTime = runningDuration == TimeSpan.MaxValue ? DateTime.MaxValue : startTime + runningDuration;

            Log("expect to stop at " + stopTime.ToString(MilliTimeFormat));
            //Thread.Sleep(Options.RunningSeconds * 1000);
            Log("passed " + (DateTime.Now - startTime).TotalSeconds + " s, thread id = " + thread.ManagedThreadId + " , state = " + thread.ThreadState + ", isAlive = " + thread.IsAlive);
            //while (DateTime.Now < stopTime)
            //{
            //    var remainSleep = (stopTime - DateTime.Now).TotalMilliseconds;
            //    Log("passed " + (DateTime.Now - startTime).TotalSeconds + " s, still need wait " + remainSleep / 1000.0 + " s. Thread id = " + thread.ManagedThreadId + " , state = " + thread.ThreadState + ", isAlive = " + thread.IsAlive);
            //    Thread.Sleep((int)remainSleep);
            //}

            var interval = Math.Max(60, options.SendInterval);
            while (true)
            {
                Thread.Sleep(interval);
                var exceedCount = 0;
                if (DateTime.Now >= stopTime || !thread.IsAlive && ConnectedTimes >= options.MaxConnectTimes)
                {
                    IsTimeout = true;
                    exceedCount++;
                }

                if (!thread.IsAlive)
                {
                    exceedCount++;
                }

                if (exceedCount == 2 || exceedCount == 1 && options.QuitIfExceededAny)
                {
                    break;
                }
            }

            //Thread.Sleep(Math.Max(100, Options.SendInterval));

            //if (thread.IsAlive && Options.MessagesPerConnection > 0)
            //{
            //    Log(string.Format("wait for connection to send {0} lines messages ...", Options.MessagesPerConnection));
            //}

            //while (thread.IsAlive && Options.MessagesPerConnection > 0 && totalSent < Options.MessagesPerConnection && !Options.QuitIfExceededAny)
            //{
            //    Thread.Sleep(60);
            //}

            Log("finished, passed " + (DateTime.Now - startTime).TotalSeconds + " s, thread id = " + thread.ManagedThreadId + " , state = " + thread.ThreadState + ", isAlive = " + thread.IsAlive);
            //if (thread.IsAlive)
            //{
            //    Log("try to kill process " + process.Id + " to stop thread id = " + thread.ManagedThreadId + " , state = " + thread.ThreadState);
            //    process.Kill();
            //    Log("try to exit to stop thread " + thread + " , state = " + thread.ThreadState);
            //    Environment.Exit(0);
            //}
        }



        private static void ListenToClient(DateTime startTime, IArgOptions options, TimeSpan runningDuration)
        {
            ConnectedTimes++;
            Log("startTime = " + startTime.ToString(MilliTimeFormat) + ", runningDuration = " + runningDuration);
            var stopTime = runningDuration == TimeSpan.MaxValue ? DateTime.MaxValue : startTime + runningDuration;
            var stopTimeText = runningDuration == TimeSpan.MaxValue ? "endless" : (startTime + runningDuration).ToString(MilliTimeFormat);
            if (DateTime.Now - startTime > runningDuration)
            {
                Log("Not running. start from " + startTime.ToString(MilliTimeFormat) + " , running for " + runningDuration);
                return;
            }
            else
            {
                Log("Machine = " + Environment.MachineName + ", OS = " + Environment.OSVersion
                    + ", start listening " + ServerSocket.LocalEndPoint.ToString()
                    + (runningDuration == TimeSpan.MaxValue ? "\t running endless " : "\t expect to stop at " + stopTimeText));
            }

            var sent = 0;
            var keys = new HashSet<String>();
            Func<bool> canQuitSending = () =>
            {
                return options.MessagesPerConnection == 0
                || options.MessagesPerConnection > 0 && sent >= options.MessagesPerConnection
                || options.KeysPerConnection > 0 && keys.Count >= options.KeysPerConnection
                ;
            };

            Action restartListen = () =>
            {
                if (DateTime.Now > stopTime || options.MaxConnectTimes > 0 && ConnectedTimes >= options.MaxConnectTimes)
                {
                    return;
                }

                Thread.Sleep(options.PauseSecondsAtDrop * 1000);
                ListenToClient(startTime, options, runningDuration);
            };

            var needRestart = false;
            try
            {
                Socket clientSocket = ServerSocket.Accept();
                var beginConnection = DateTime.Now;
                while (true)
                {
                    if (options.MessagesPerConnection > 0 && sent >= options.MessagesPerConnection)
                    {
                        needRestart = !options.QuitIfExceededAny;
                        break;
                    }
                    else if (IsTimeout)
                    {
                        break;
                    }

                    if (DateTime.Now - startTime > runningDuration)
                    {
                        Log("Stop running. start from " + startTime.ToString(MilliTimeFormat) + " , running for " + runningDuration);
                        break;
                    }

                    sent++;
                    TotalSentMessages++;
                    var now = DateTime.Now;
                    keys.Add(now.ToString(TimeFormat));
                    if(options.KeysPerConnection > 0 && keys.Count > options.KeysPerConnection)
                    {
                        needRestart = !options.QuitIfExceededAny;
                        break;
                    }
                    var message = string.Format("{0} from '{1}' '{2}' {3} times[{4}] send[{5}] keys[{6}] to {7}{8}",
                        now.ToString(MicroTimeFormat), Environment.OSVersion, Environment.MachineName, HostAddress, ConnectedTimes, 
                        sent, keys.Count, clientSocket.RemoteEndPoint, Environment.NewLine);
                    Console.Write(message);
                    clientSocket.Send(Encoding.ASCII.GetBytes(message));
                    Thread.Sleep(options.SendInterval);
                }

                Log(string.Format("close client : {0} , connection from {1} to {2}, used {3} s, sent {4} lines, keys = {5}", 
                    clientSocket.RemoteEndPoint, beginConnection.ToString(MilliTimeFormat), 
                    DateTime.Now.ToString(MilliTimeFormat), (DateTime.Now - beginConnection).TotalSeconds,
                    sent, keys.Count
                    ));
                clientSocket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                needRestart = true;
            }

            if (needRestart)
            {
                restartListen();
            }

        }

        private static IPAddress GetHost(bool print = false)
        {
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
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
        public static ArgType GetArgValue<ArgType>(ref int index, string[] args, string argName, ArgType defaultValue, bool canBeOmitted = true)
        {
            index++;
            if (args.Length > index)
            {
                Console.WriteLine("args[{0}] : {1} = {2}", index, argName, args[index]);
                var argValue = args[index];
                if (defaultValue is bool)
                {
                    argValue = Regex.IsMatch(args[index], "1|true", RegexOptions.IgnoreCase).ToString();
                }

                return (ArgType)TypeDescriptor.GetConverter(typeof(ArgType)).ConvertFromString(argValue);
            }
            else if (canBeOmitted)
            {
                Console.WriteLine("args[{0}] : {1} = {2}", index, argName, defaultValue);
                return defaultValue;
            }
            else
            {
                throw new ArgumentException(string.Format("must set {0} at arg[{0}]", argName, index + 1), argName);
            }
        }
    }
}
