using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SourceLinesSocket
{
    class SourceLinesSocket
    {
        private static Socket serverSocket;
        private static IPAddress hostAddress;

        private static volatile bool IsNeedStop = false;

        const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
        const string MilliTimeFormat = TimeFormat + ".fff";
        const string MicroTimeFormat = MilliTimeFormat + "fff";

        static void Log(string message)
        {
            Console.WriteLine("{0} SourceLinesSocket : {1}", DateTime.Now.ToString(MilliTimeFormat), message);
        }

        static void Main(string[] args)
        {
            var exe = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var exeName = Path.GetFileName(exe);

            if (args.Length < 1 || args[0] == "-h" || args[0] == "--help")
            {
                Console.WriteLine("Usage :    {0}  port  [send-interval-milliseconds]   [running-seconds]  [bind-host]  [send-per-connection]", exeName);
                Console.WriteLine("Example-1: {0}  9111  100                            3600               127.0.0.1     0", exeName);
                Console.WriteLine("Example-2: {0}  9111  100  3600 {1}", exeName, GetHost(false));
                return;
            }

            int idxArg = -1;
            var port = GetArgValue(ref idxArg, args, "port", 9111, false);
            var milliInterval = GetArgValue(ref idxArg, args, "milliseconds-send-interval", 100);
            var runningSeconds = GetArgValue(ref idxArg, args, "running-seconds", 3600);
            var hostToUse = GetArgValue(ref idxArg, args, "host-to-force-use", "127.0.0.1");
            var sendPerConnection = GetArgValue(ref idxArg, args, "send-lines-per-connection", 0);

            var runningDuration = runningSeconds <= 0 ? TimeSpan.MaxValue : new TimeSpan(0, 0, 0, runningSeconds);

            hostAddress = !string.IsNullOrWhiteSpace(hostToUse) ? IPAddress.Parse(hostToUse) : GetHost();
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(hostAddress, port));
            serverSocket.Listen(10);

            var startTime = DateTime.Now;
            var thread = new Thread(() => { ListenToClient(startTime, milliInterval, runningDuration, sendPerConnection); });
            thread.IsBackground = runningSeconds > 0;
            thread.Start();

            if (runningSeconds < 1)
            {
                return;
            }

            var process = Process.GetCurrentProcess();
            var stopTime = startTime + runningDuration;

            Log("expect to stop at " + stopTime.ToString(MilliTimeFormat));
            Thread.Sleep(runningSeconds * 1000);
            Log("passed " + (DateTime.Now - startTime).TotalSeconds + " s, thread id = " + thread.ManagedThreadId + " , state = " + thread.ThreadState + ", isAlive = " + thread.IsAlive);
            while (DateTime.Now < stopTime)
            {
                var remainSleep = (stopTime - DateTime.Now).TotalMilliseconds;
                Log("passed " + (DateTime.Now - startTime).TotalSeconds + " s, still need wait " + remainSleep / 1000.0 + " s. Thread id = " + thread.ManagedThreadId + " , state = " + thread.ThreadState + ", isAlive = " + thread.IsAlive);
                Thread.Sleep((int)remainSleep);
            }

            IsNeedStop = true;
            Thread.Sleep(Math.Max(100, milliInterval));

            if (thread.IsAlive && sendPerConnection > 0)
            {
                Log(string.Format("wait for connection to send {0} lines messages ...", sendPerConnection));
            }

            while (thread.IsAlive)
            {
                Thread.Sleep(60);
            }

            Log("finished, passed " + (DateTime.Now - startTime).TotalSeconds + " s, thread id = " + thread.ManagedThreadId + " , state = " + thread.ThreadState + ", isAlive = " + thread.IsAlive);
            //if (thread.IsAlive)
            //{
            //    Log("try to kill process " + process.Id + " to stop thread id = " + thread.ManagedThreadId + " , state = " + thread.ThreadState);
            //    process.Kill();
            //    Log("try to exit to stop thread " + thread + " , state = " + thread.ThreadState);
            //    Environment.Exit(0);
            //}
        }

        private static void ListenToClient(DateTime startTime, int milliInterval, TimeSpan runningDuration, int sendPerConnection, int times = 1)
        {
            var stopTime = runningDuration == TimeSpan.MaxValue ? "endless" : (startTime + runningDuration).ToString(MilliTimeFormat);
            if (DateTime.Now - startTime > runningDuration)
            {
                Log("Not running. start from " + startTime.ToString(MilliTimeFormat) + " , running for " + runningDuration);
                return;
            }
            else
            {
                Log("Machine = " + Environment.MachineName + ", OS = " + Environment.OSVersion
                    + ", start listening " + serverSocket.LocalEndPoint.ToString()
                    + (runningDuration == TimeSpan.MaxValue ? "\t running endless " : "\t expect to stop at " + stopTime));
            }

            var sent = 0;
            Func<bool> canQuitSending = () =>
            {
                return sendPerConnection == 0 || sendPerConnection > 0 && sent >= sendPerConnection;
            };

            try
            {
                Socket clientSocket = serverSocket.Accept();
                var beginConnection = DateTime.Now;
                while (true)
                {
                    if(sendPerConnection > 0 && sent >= sendPerConnection)
                    {
                        break;
                    }
                    else if (IsNeedStop) // && canQuitSending())
                    {
                        break;
                    }

                    if (DateTime.Now - startTime > runningDuration) // && canQuitSending())
                    {
                        Log("Stop running. start from " + startTime.ToString(MilliTimeFormat) + " , running for " + runningDuration);
                        break;
                    }

                    sent++;
                    var message = string.Format("{0} from '{1}' '{2}' {3} times[{4}] send[{5}] to {6}{7}",
                        DateTime.Now.ToString(MicroTimeFormat), Environment.OSVersion, Environment.MachineName, hostAddress, times, sent, clientSocket.RemoteEndPoint, Environment.NewLine);
                    Console.Write(message);
                    clientSocket.Send(Encoding.ASCII.GetBytes(message));
                    Thread.Sleep(milliInterval);
                }
                Log(string.Format("close client : {0} , connection from {1} to {2}, used {3} s", clientSocket.RemoteEndPoint, beginConnection.ToString(MilliTimeFormat), DateTime.Now.ToString(MilliTimeFormat), (DateTime.Now - beginConnection).TotalSeconds));
                clientSocket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                var thread = new Thread(() => { ListenToClient(startTime, milliInterval, runningDuration, sendPerConnection, times + 1); });
                thread.Start();
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
