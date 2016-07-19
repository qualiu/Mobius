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
    public class UnionTopicTestOptions : ArgOptions
    {
        [ArgDefaultValue(""), ArgDescription("Kafka topic name, row content = " + nameof(RowIdCountTime)), ArgRequired(), ArgRegex(@"^\S+")]
        public string Topic1 { get; set; }

        [ArgDefaultValue(""), ArgDescription("Kafka topic name, row content = " + nameof(RowIdCountTime)), ArgRequired(), ArgRegex(@"^\S+")]
        public string Topic2 { get; set; }

        [ArgDefaultValue("30"), ArgDescription("Print final count")]
        public int PrintCount { get; set; }
    }

    class UnionTopicTest : TestKafkaBase<UnionTopicTest, UnionTopicTestOptions>
    {
        public override void Run(String[] args, Lazy<SparkContext> sparkContext)
        {
            if (!ParseArgs(args))
            {
                return;
            }

            var options = Options as UnionTopicTestOptions;

            var streamingContext = StreamingContext.GetOrCreate(options.CheckPointDirectory,
                () =>
                {
                    var ssc = new StreamingContext(sparkContext.Value, options.SlideSeconds);
                    ssc.Checkpoint(options.CheckPointDirectory);

                    var stream1 = KafkaUtils.CreateDirectStream(ssc, new List<string> { options.Topic1 }, kafkaParams, offsetsRange)
                        .Map(line => new RowIdCountTime().Deserialize(line.Value));
                    var stream2 = KafkaUtils.CreateDirectStream(ssc, new List<string> { options.Topic2 }, kafkaParams, offsetsRange)
                        .Map(line => new RowIdCountTime().Deserialize(line.Value));
                    var stream = stream1.Union(stream2);
                    var count = stream.Count();
                    Logger.LogInfo("Will print count : ");
                    count.Print(options.PrintCount);
                    return ssc;
                });

            streamingContext.Start();

            if (options.RunningSeconds > 0)
            {
                streamingContext.AwaitTerminationOrTimeout(options.RunningSeconds * 1000);
            }

            Logger.LogInfo("Final sumCount : {0}", SumCountStatic.GetStaticSumCount().ToString());
        }
    }
}
