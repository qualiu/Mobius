using System.Collections.Generic;
using System.IO;
using System.Text;
using CommonTestUtils;
using Microsoft.Spark.CSharp.Core;
using Microsoft.Spark.CSharp.Streaming;

namespace kafkaStreamTest
{
    class kafkaStreamTest : BaseTestUtil<kafkaStreamTest>
    {
        static void Main(string[] args)
        {
            var parsedOK = false;
            var options = ArgParser.Parse<ArgOptions>(args, out parsedOK);
            if (!parsedOK)
            {
                return;
            }

            if (options.DeleteCheckPointDirectory)
            {
                TestUtils.DeleteDirectory(options.CheckPointDirectory);
            }

            var sparkContext = new SparkContext(new SparkConf().SetAppName(typeof(kafkaStreamTest).Name));
            var topicList = new List<string> { options.topic };

            var kafkaParams = new Dictionary<string, string>
            {
                { "group.id",  options.groupId.ToString() },
                { "metadata.broker.list" ,  options.brokerList.ToString() },
                { "auto.offset.reset" ,  options.autoOffset.ToString() },
                //{"zookeeper.session.timeout.ms" ,  "200" },
                //{"zookeeper.sync.time.ms" -> "6000"
                //{ "auto.commit.interval.ms" ,  "1000" },
                //{ "serializer.class" -> "kafka.serializer.StringEncoder"
                {"zookeeper.connect" ,  options.zookeeper.ToString() },
                { "zookeeper.connection.timeout.ms",  "1000" }
            };

            var offsetsRange = new Dictionary<string, long>
            {

            };

            var streamingContext = StreamingContext.GetOrCreate(options.CheckPointDirectory,
                () =>
                {
                    var ssc = new StreamingContext(sparkContext, options.SlideSeconds);
                    ssc.Checkpoint(options.CheckPointDirectory);

                    var stream = KafkaUtils.CreateDirectStream(ssc, topicList, kafkaParams, offsetsRange)
                        .Map(line => Encoding.UTF8.GetString(line.Value));

                    var pairs = stream.Map(new ParseKeyValueArray().Parse);

                    var reducedStream = pairs.ReduceByKeyAndWindow(
                        new ReduceHelper(options.CheckArray).Sum,
                        new ReduceHelper(options.CheckArray).InverseSum,
                        options.WindowSeconds,
                        options.SlideSeconds);

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

            Log("Final sumCount : {0}", SumCountStatic.GetStaticSumCount().ToString());
        }
    }
}
