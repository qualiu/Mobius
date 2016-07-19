using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTestUtils;
using Microsoft.Spark.CSharp.Core;
using Microsoft.Spark.CSharp.Streaming;
using PowerArgs;

namespace kafkaStreamTest
{
    [Serializable]
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public class WindowSlideTestOptions : ArgOptions
    {
        [ArgDefaultValue("test"), ArgDescription("Kafka topic names (separated by ',' if multiple)"), ArgRequired()]
        public String Topics { get; set; }

        [ArgShortcut("a"), ArgDefaultValue(true), ArgDescription("is value type array")]
        public bool IsArrayValue { get; set; }

        [ArgShortcut("u"), ArgDefaultValue(false), ArgDescription("is uneven array value")]
        public bool IsUnevenArray { get; set; }

        [ArgShortcut("e"), ArgDefaultValue(0), ArgDescription("element count in value array. 0 means not set.")]
        public long ElementCount { get; set; }

        [ArgShortcut("k"), ArgDefaultValue(true), ArgDescription("check array before operation such as reduce.")]
        public bool CheckArrayAtFirst { get; set; }

        [ArgShortcut("v"), ArgDefaultValue(-1), ArgDescription("line count to validate with, ignore if < 0 ")]
        public Int64 ValidateCount { get; set; }

        [ArgDefaultValue(true), ArgDescription("show received lines text")]
        public bool ShowReceivedLines { get; set; }
    }

    class WindowSlideTest : TestKafkaBase<WindowSlideTest, WindowSlideTestOptions>
    {
        public override void Run(String[] args, Lazy<SparkContext> sparkContext)
        {
            if (!ParseParameters(args))
            {
                return;
            }

            var options = Options as WindowSlideTestOptions;

            var streamingContext = StreamingContext.GetOrCreate(options.CheckPointDirectory,
                () =>
                {
                    var ssc = new StreamingContext(sparkContext.Value, options.SlideSeconds);
                    ssc.Checkpoint(options.CheckPointDirectory);

                    var stream = KafkaUtils.CreateDirectStream(ssc, topicList, kafkaParams, offsetsRange)
                        .Map(line => Encoding.UTF8.GetString(line.Value));

                    var pairs = stream.Map(new ParseKeyValueArray(options.ElementCount, options.ShowReceivedLines).Parse);

                    var reducedStream = pairs.ReduceByKeyAndWindow(
                        new ReduceHelper(options.CheckArrayAtFirst).Sum,
                        new ReduceHelper(options.CheckArrayAtFirst).InverseSum,
                        options.WindowSeconds,
                        options.SlideSeconds
                        );

                    reducedStream.ForeachRDD(new SumCountStatic().ForeachRDD<int[]>);

                    if (!string.IsNullOrWhiteSpace(options.SaveTxtDirectory))
                    {
                        reducedStream.SaveAsTextFiles(Path.Combine(options.SaveTxtDirectory, typeof(kafkaStreamTest).Name), ".txt");
                    }

                    return ssc;
                });

            streamingContext.Start();

            if (options.RunningSeconds > 0)
            {
                streamingContext.AwaitTerminationOrTimeout(options.RunningSeconds * 1000);
            }

            Logger.LogInfo("Final sumCount : {0}", SumCountStatic.GetStaticSumCount().ToString());
        }


        protected List<string> topicList;

        protected virtual List<string> GetTopics(WindowSlideTestOptions options)
        {
            return new List<string>(options.Topics.Split(";,".ToArray()));
        }

        protected virtual bool ParseParameters(String[] args)
        {
            if (!ParseArgs(args))
            {
                return false;
            }

            topicList = GetTopics(Options);

            kafkaParams = GetKafkaParameters(Options);

            offsetsRange = GetOffsetRanges(Options);

            if (Options.DeleteCheckPointDirectory)
            {
                TestUtils.DeleteDirectory(Options.CheckPointDirectory);
            }

            return true;
        }

    }
}
