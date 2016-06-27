using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Spark.CSharp.Core;
using Microsoft.Spark.CSharp.Streaming;
using Microsoft.Spark.CSharp.Interop;
using Microsoft.Spark.CSharp.Services;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Collections;

namespace testKeyValueStream
{
    [Serializable]
    public class testKeyValueStream
    {
        static string host = "127.0.0.1";
        static int port = 9111;
        static int batchSeconds = 1;
        static int windowSeconds = 4;
        static int slideSeconds = 2;
        static int runningSeconds = 30;
        static int totalTestTimes = 20;
        static string checkpointDirectory = "checkDir";
        static bool isReduceByKeyAndWindow = false;
        static bool isValueArray = true;
        static bool isUnevenArray = false;
        static long valueElements = 1024 * 1024 * 20;
        static bool needSaveToTxtFile = false;
        static bool checkBeforeSum = true;

        //static SumCount sumCount = new SumCount();

        public static void Main(string[] args)
        {
            var exeName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);

            // TestReceivedLine();
            if (args.Length < 1 || args[0] == "-h" || args[0] == "--help")
            {
                Console.WriteLine("Usage :    {0}  Host       Port [batchSeconds] [windowSeconds] [slideSeconds] [runningSeconds] [testTimes] [checkpointDirectory] [isReduceByKeyAndWindow] [isValueArray] [isUnevenArray] [valueArrayElements] [saveTxt] [checkArrayBeforeReduce]", exeName);
                Console.WriteLine("Example-1: {0}  127.0.0.1  9111   1               30               20         checkDir               0                        1          0                  1048576              0         1", exeName);
                Console.WriteLine("Example-2: " + exeName + " " + TestUtils.GetHost() + " " + port + " " + batchSeconds + " " + windowSeconds + " " + slideSeconds + " " + runningSeconds + " " + totalTestTimes
                    + " " + checkpointDirectory + " " + isReduceByKeyAndWindow + " " + isValueArray + " " + isUnevenArray + " " + valueElements + " " + needSaveToTxtFile + " " + checkBeforeSum);
                Console.WriteLine("Example-3: " + exeName + " " + host + " " + port + " " + batchSeconds + " " + windowSeconds + " " + slideSeconds + " " + runningSeconds + " " + totalTestTimes
                    + " " + checkpointDirectory + " " + Convert.ToInt32(isReduceByKeyAndWindow) + " " + Convert.ToInt32(isValueArray) + " " + Convert.ToInt32(isUnevenArray) + " " + valueElements + " " + Convert.ToInt32(needSaveToTxtFile) + " " + Convert.ToInt32(checkBeforeSum));
                Console.WriteLine("The above host and port are from a tool : SourceLinesSocket in this project.");
                return;
            }

            var thisType = typeof(testKeyValueStream).Name;
            int idxArg = -1;
            host = TestUtils.GetArgValue(thisType, ref idxArg, args, "host", host, false);
            port = TestUtils.GetArgValue(thisType, ref idxArg, args, "port", port, false);
            batchSeconds = TestUtils.GetArgValue(thisType, ref idxArg, args, "batchSeconds", batchSeconds);
            slideSeconds = TestUtils.GetArgValue(thisType, ref idxArg, args, "slideSeconds", slideSeconds);
            windowSeconds = TestUtils.GetArgValue(thisType, ref idxArg, args, "windowSeconds", windowSeconds);
            runningSeconds = TestUtils.GetArgValue(thisType, ref idxArg, args, "runningSeconds", runningSeconds);
            totalTestTimes = TestUtils.GetArgValue(thisType, ref idxArg, args, "totalTestTimes", totalTestTimes);
            checkpointDirectory = TestUtils.GetArgValue(thisType, ref idxArg, args, "checkpointDirectory", checkpointDirectory);
            isReduceByKeyAndWindow = TestUtils.GetArgValue(thisType, ref idxArg, args, "isReduceByKeyAndWindow", isReduceByKeyAndWindow);
            isValueArray = TestUtils.GetArgValue(thisType, ref idxArg, args, "isValueArray", isValueArray);
            isUnevenArray = TestUtils.GetArgValue(thisType, ref idxArg, args, "isUnevenArray", isUnevenArray);
            valueElements = TestUtils.GetArgValue(thisType, ref idxArg, args, "valueArrayElements", valueElements);
            needSaveToTxtFile = TestUtils.GetArgValue(thisType, ref idxArg, args, "needSaveToTxtFile", needSaveToTxtFile);
            checkBeforeSum = TestUtils.GetArgValue(thisType, ref idxArg, args, "checkBeforeSum", checkBeforeSum);

            Log("will connect " + host + ":" + port + " batchSeconds = " + batchSeconds + " s , windowSeconds = " + windowSeconds + " s, slideSeconds = " + slideSeconds + " s."
                + " checkpointDirectory = " + checkpointDirectory + ", is-array-test = " + isValueArray);

            var prefix = exeName + (isValueArray ? "-array" + (isUnevenArray ? "-uneven" : "-even") : "-single") + "-";
            var sc = new SparkContext(new SparkConf().SetAppName(prefix));

            var beginTime = DateTime.Now;

            Action<long> testOneStreaming = (testTime) =>
            {
                var timesInfo = " test[" + testTime + "]-" + totalTestTimes + " ";
                Log("============== begin of " + timesInfo + " =========================");
                var ssc = new StreamingContext(sc, batchSeconds);
                ssc.Checkpoint(checkpointDirectory);
                var lines = ssc.SocketTextStream(host, port, StorageLevelType.MEMORY_AND_DISK_SER);

                var sumCount = new SumCount();

                StartOneTest(sc, lines, valueElements, sumCount, prefix);
                sumCount = new SumCountStaticHelper().GetSumCount();

                ssc.Start();
                var startTime = DateTime.Now;
                ssc.AwaitTerminationOrTimeout(runningSeconds * 1000);
                //Log(string.Format("============= end of {0}, start from {1} , used {2} s. total cost {3} s. final sumCount = { {4} }",
                Log(string.Format("============= end of {0}, start from {1} , used {2} s. total cost {3} s. final sumCount : {4} ",
                    timesInfo, startTime.ToString(TestUtils.MilliTimeFormat), (DateTime.Now - startTime).TotalSeconds, (DateTime.Now - beginTime).TotalSeconds, sumCount));
                ssc.Stop();
            };


            for (var times = 0; times < totalTestTimes; times++)
            {
                testOneStreaming(times + 1);
            }

            Log("finished all test , total test times = " + totalTestTimes + ", used time = " + (DateTime.Now - beginTime));
        }

        static void Log(string message)
        {
            Console.WriteLine("{0} {1} : {2}", TestUtils.NowMilli, typeof(testKeyValueStream).Name, message);
        }

        static void StartOneTest(SparkContext sc, DStream<string> lines, long elements, SumCount sumCount, string prefix, string suffix = ".txt")
        {
            if (!isValueArray)
            {
                //var pairs = lines.Map(line => new ParseKeyValue(0).Parse(line));
                var pairs = lines.Map(new ParseKeyValue(0).Parse);
                var reducedStream = isReduceByKeyAndWindow ? pairs.ReduceByKeyAndWindow(Sum, InverseSum, windowSeconds, slideSeconds)
                    : pairs.ReduceByKey(Sum);
                ForEachRDD("KeyValue", reducedStream, sumCount, prefix, suffix);
            }
            else if (isUnevenArray)
            {
                //var pairs = lines.Map(line => new ParseKeyValueUnevenArray(elements).Parse(line));
                var pairs = lines.Map(new ParseKeyValueUnevenArray(elements).Parse);
                var reducedStream = isReduceByKeyAndWindow ? pairs.ReduceByKeyAndWindow(Sum, InverseSum, windowSeconds, slideSeconds)
                    : pairs.ReduceByKey(Sum);
                ForEachRDD("KeyValueUnevenArray", reducedStream, sumCount, prefix, suffix);

            }
            else
            {
                Log("StartOneTest valueElements = " + elements);
                //var pairs = lines.Map(line => new ParseKeyValueArray(elements).Parse(line));
                var pairs = lines.Map(new ParseKeyValueArray(elements).Parse);
                var reducedStream = isReduceByKeyAndWindow ? pairs.ReduceByKeyAndWindow(Sum, InverseSum, windowSeconds, slideSeconds)
                    : pairs.ReduceByKey(Sum);
                ForEachRDD("KeyValueEvenArray", reducedStream, sumCount, prefix, suffix);
            }
        }

        public static void ForEachRDD<V>(string title, DStream<KeyValuePair<string, V>> reducedStream, SumCount sumCount, string prefix, string suffix = ".txt")
        {
            Log("ForEachRDD " + title);
            reducedStream.ForeachRDD(new SumCountStaticHelper().ForeachRDD<V>);
            // reducedStream.ForeachRDD(new SumCountHelper(sumCount).ForeachRDD<V>);

            //reducedStream.ForeachRDD((time, rdd) => new SumCountHelper(sumCount).Execute<V>(time, rdd));

            //reducedStream.ForeachRDD((time, rdd) =>
            //{
            //    sumCount.RddCount += 1;
            //    var taken = rdd.Collect();
            //    Console.WriteLine("{0} taken.length = {1} , taken = {2}", TestUtils.NowMilli, taken.Length, taken);

            //    foreach (object record in taken)
            //    {
            //        sumCount.RecordCount += 1;
            //        KeyValuePair<string, V> kv = (KeyValuePair<string, V>)record;
            //        Console.WriteLine("{0} record: key = {2}, value = {3}", TestUtils.NowMilli, record, kv.Key, GetValueText(kv.Value));
            //        sumCount.LineCount += GetFirstElementValue(kv.Value);
            //    }
            //});

            //Log(string.Format("{0} reducedStream.Count = {1}", title, reducedStream.Count()));

            if (needSaveToTxtFile)
            {
                reducedStream.SaveAsTextFiles(prefix, suffix);
            }
        }
        
        static int[] Sum(int[] a, int[] b)
        {
            Log(string.Format("SumArray a{0}, b{1}", TestUtils.ArrayToText(a), TestUtils.ArrayToText(b)));

            if (checkBeforeSum)
            {
                if (a == null || b == null)
                {
                    return a == null ? b : a;
                }

                if (a.Length == 0 || b.Length == 0)
                {
                    return a.Length == 0 ? b : a;
                }
            }

            var count = checkBeforeSum ? Math.Min(a.Length, b.Length) : a.Length;
            var c = new int[count];
            for (var k = 0; k < c.Length; k++)
            {
                c[k] = a[k] + b[k];
            }

            return c;
        }

        static int Sum(int a, int b)
        {
            Log(string.Format("InverseSum : a - b = {0} - {1}", a, b));
            return a + b;
        }

        static int[] InverseSum(int[] a, int[] b)
        {
            Log(string.Format("InverseSumArray a{0}, b{1}", TestUtils.ArrayToText(a), TestUtils.ArrayToText(b)));
            if (checkBeforeSum)
            {
                if (a == null || b == null)
                {
                    return a == null ? b : a;
                }

                if (a.Length == 0 || b.Length == 0)
                {
                    return a.Length == 0 ? b : a;
                }
            }

            var count = checkBeforeSum ? Math.Min(a.Length, b.Length) : a.Length;
            var c = new int[count];
            for (var k = 0; k < c.Length; k++)
            {
                c[k] = a[k] - b[k];
            }
            return c;
        }

        static int InverseSum(int a, int b)
        {
            Log(string.Format("InverseSum : a - b = {0} - {1}", a, b));
            return a - b;
        }
    }
}
