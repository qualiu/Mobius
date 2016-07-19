using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTestUtils;
using log4net;
using Microsoft.Spark.CSharp.Core;
using Microsoft.Spark.CSharp.Services;

namespace kafkaStreamTest
{
    public interface ITestKafkaBase
    {
        bool ParseArgs(string[] args);
        void Run(String[] args, Lazy<SparkContext> sparkContext);
    }

    public abstract class TestKafkaBase<ClassName, ArgClass> : ITestKafkaBase
        where ArgClass : class, new()
    {
        protected static readonly ILoggerService Logger = LoggerServiceFactory.GetLogger(typeof(ClassName));

        protected dynamic Options;

        public abstract void Run(String[] args, Lazy<SparkContext> sparkContext);

        public virtual bool ParseArgs(string[] args)
        {
            var parsedOK = false;
            Options = ArgParser.Parse<ArgClass>(args, out parsedOK);
            return parsedOK;
        }

        protected Dictionary<string, string> kafkaParams;

        protected Dictionary<string, long> offsetsRange;

        protected virtual Dictionary<string, long> GetOffsetRanges(ArgOptions options)
        {
            var offsetsRange = new Dictionary<string, long>();
            if (options.FromOffset >= 0)
            {
                offsetsRange.Add("fromOffset", options.FromOffset);
            }

            if (options.UntilOffset >= 0)
            {
                offsetsRange.Add("untilOffset", options.UntilOffset);
            }

            return offsetsRange;

        }
        protected virtual Dictionary<string, string> GetKafkaParameters(ArgOptions options)
        {
            return new Dictionary<string, string>
            {
                { "group.id",  options.GroupId.ToString() },
                { "metadata.broker.list",  options.BrokerList.ToString() },
                { "auto.offset.reset",  options.AutoOffset.ToString() },
                { "zookeeper.connect",  options.Zookeeper.ToString() },
                { "zookeeper.connection.timeout.ms",  "1000" },
                //{"zookeeper.session.timeout.ms" ,  "200" },
                //{"zookeeper.sync.time.ms" -> "6000"
                //{ "auto.commit.interval.ms" ,  "1000" },
                //{ "serializer.class" -> "kafka.serializer.StringEncoder"
            };
        }

    }
}
