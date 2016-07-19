using System;
using System.Collections.Generic;
using System.IO;
using CommonTestUtils;
using Microsoft.Spark.CSharp.Core;
using Microsoft.Spark.CSharp.Streaming;

namespace testKeyValueStream
{
    [Serializable]
    public class testKeyValueStream : BaseTestUtilLog<testKeyValueStream>
    {
        private static ArgOptions Options = null;

        public static void Main(string[] args)
        {
            var config = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            Logger.LogDebug("{0} logger configuration {1}", File.Exists(config) ? "Exist" : "Not Exist", config);
            var isParseOK = false;
            //Options = ParserByCommandLine.Parse(args, out isParseOK);
            Options = ArgParser.Parse<ArgOptions>(args, out isParseOK, "-Help");

            if (!isParseOK)
            {
                return;
            }

            Logger.LogInfo("will connect " + Options.Host + ":" + Options.Port + " batchSeconds = " + Options.BatchSeconds + " s , windowSeconds = " + Options.WindowSeconds + " s, slideSeconds = " + Options.SlideSeconds + " s."
                + " checkpointDirectory = " + Options.CheckPointDirectory + ", is-array-test = " + Options.IsArrayValue);

            if (Options.DeleteCheckPointDirectory)
            {
                TestUtils.DeleteDirectory(Options.CheckPointDirectory);
            }

            var prefix = ExeName + (Options.IsArrayValue ? "-array" + (Options.IsUnevenArray ? "-uneven" : "-even") : "-single");
            var sc = new SparkContext(new SparkConf().SetAppName(prefix));

            var beginTime = DateTime.Now;

            Action<long> testOneStreaming = (testTime) =>
            {
                var timesInfo = " test[" + testTime + "]-" + Options.TestTimes + " ";
                Logger.LogInfo("============== Begin of " + timesInfo + " =========================");
                var ssc = new StreamingContext(sc, Options.BatchSeconds);
                ssc.Checkpoint(Options.CheckPointDirectory);
                var lines = ssc.SocketTextStream(Options.Host, Options.Port, StorageLevelType.MEMORY_AND_DISK_SER);


                var oldSum = new SumCount(SumCountStatic.GetStaticSumCount());
                StartOneTest(sc, lines, Options.ElementCount, prefix);
                var newSum = SumCountStatic.GetStaticSumCount();
                // var sum = newSum - oldSum; // newSum maybe same as oldSum

                ssc.Start();
                var startTime = DateTime.Now;
                ssc.AwaitTerminationOrTimeout(Options.RunningSeconds * 1000);
                ssc.Stop();

                var sum = newSum - oldSum;
                var isValidationOK = Options.ValidateCount <= 0 || Options.ValidateCount == sum.LineCount;
                var validationMessage = Options.ValidateCount <= 0 ? string.Empty :
                    (isValidationOK ? ". Validation OK" : string.Format(". Validation failed : expected = {0}, but line count = {1}", Options.ValidateCount, sum.LineCount));

                Logger.LogInfo("oldSum = {0}, newSum = {1}, sum = {2}", oldSum, newSum, sum);
                Logger.LogInfo("============= End of {0}, start from {1} , used {2} s. total cost {3} s. Reduced final sumCount : {4} {5}",
                    timesInfo, startTime.ToString(TestUtils.MilliTimeFormat), (DateTime.Now - startTime).TotalSeconds,
                    (DateTime.Now - beginTime).TotalSeconds, sum.ToString(), validationMessage);

                if (!isValidationOK)
                {
                    Options.OutArgs((name, value) => Logger.LogInfo("Trace arg : {0} = {1}", name, value));
                    throw new Exception(validationMessage);
                }
            };

            for (var times = 0; times < Options.TestTimes; times++)
            {
                testOneStreaming(times + 1);
            }

            Logger.LogInfo("finished all test , total test times = " + Options.TestTimes + ", used time = " + (DateTime.Now - beginTime));
        }

        static void StartOneTest(SparkContext sc, DStream<string> lines, long elements, string prefix, string suffix = ".txt")
        {
            var isReduceByKey = Options.IsReduceByKey();
            Logger.LogDebug("isReduceByKey = {0}", isReduceByKey);
            if (!Options.IsArrayValue)
            {
                //var pairs = lines.Map(line => new ParseKeyValue(0).Parse(line));
                var pairs = lines.Map(new ParseKeyValue(0).Parse);
                var reducedStream = isReduceByKey ? pairs.ReduceByKey(Sum)
                    : pairs.ReduceByKeyAndWindow(Sum, InverseSum, Options.WindowSeconds, Options.SlideSeconds);
                ForEachRDD("KeyValue", reducedStream, prefix, suffix);
            }
            else
            {

                //var pairs = lines.Map(line => new ParseKeyValueUnevenArray(elements).Parse(line));
                var pairs = Options.IsUnevenArray ? lines.Map(new ParseKeyValueUnevenArray(elements).Parse) : lines.Map(new ParseKeyValueArray(elements).Parse);
                var reducedStream = isReduceByKey ? pairs.ReduceByKey(new ReduceHelper(Options.CheckArray).Sum)
                    : pairs.ReduceByKeyAndWindow(new ReduceHelper(Options.CheckArray).Sum, new ReduceHelper(Options.CheckArray).InverseSum, Options.WindowSeconds, Options.SlideSeconds);
                ForEachRDD(Options.IsUnevenArray ? "KeyValueUnevenArray" : "KeyValueArray", reducedStream, prefix, suffix);
            }
        }

        public static void ForEachRDD<V>(string title, DStream<KeyValuePair<string, V>> reducedStream, string prefix, string suffix = ".txt")
        {
            Logger.LogDebug("ForEachRDD " + title);
            reducedStream.ForeachRDD(new SumCountStatic().ForeachRDD<V>);

            //reducedStream.ForeachRDD(new SumCountHelper(sumCount).ForeachRDD<V>);
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

            if (!string.IsNullOrWhiteSpace(Options.SaveTxtDirectory))
            {
                reducedStream.Map(kv => $"{kv.Key} = {TestUtils.GetValueText(kv.Value)}").SaveAsTextFiles(Path.Combine(Options.SaveTxtDirectory, prefix), suffix);
            }
        }


        static int Sum(int a, int b)
        {
            Logger.LogDebug("InverseSum : a - b = {0} - {1}", a, b);
            return a + b;
        }

        static int InverseSum(int a, int b)
        {
            Logger.LogDebug("InverseSum : a - b = {0} - {1}", a, b);
            return a - b;
        }

    }
}
