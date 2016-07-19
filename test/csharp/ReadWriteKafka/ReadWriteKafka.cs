using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTestUtils;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using PowerArgs;

namespace ReadWriteKafka
{
    [Serializable]
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public class ArgReadWriteKafka
    {
        [ArgRequired(), ArgDefaultValue("http://localhost:9092"), ArgDescription("Kafka metadata.broker.list (separated by ';' if multiple)")]
        public String BrokerList { get; set; }

        [ArgDefaultValue("id_user"), ArgDescription("Kafka topic to write for id-user")]
        public String TopicIdUser { get; set; }

        [ArgDefaultValue("id_count"), ArgDescription("Kafka topic to write for id-count")]
        public String TopicIdCount { get; set; }

        [ArgDefaultValue(60), ArgDescription("Read/Write row count")]
        public long Rows { get; set; }

        [ArgDefaultValue(false), ArgDescription("Is writing message to Kafka (or else reading)")]
        public bool IsWrite { get; set; }

        [ArgDefaultValue(""), ArgDescription("Topic to read")]
        public string ReadTopic { get; set; }

        [ArgDefaultValue(false), ArgDescription("Display row header info")]
        public bool ShowRowHeader { get; set; }

        //[ArgDefaultValue(0), ArgDescription("Kafka connection timeout milliseconds")]
        //public int KafkaTimeout { get; set; }
    }

    class ReadWriteKafka : BaseTestUtilLog4Net<ReadWriteKafka>
    {
        private static Random random = new Random();
        static void Main(string[] args)
        {
            var parsedOK = false;
            var options = ArgParser.Parse<ArgReadWriteKafka>(args, out parsedOK);
            if (!parsedOK)
            {
                return;
            }

            var brokers = options.BrokerList.Split(";,".ToCharArray());
            var brokersUriList = new List<Uri>(brokers.Select(broker => new Uri(broker)));
            if (options.IsWrite)
            {
                WriteTestData(brokersUriList, options);
            }
            else
            {
                ReadData(brokersUriList, options);
            }
        }

        static long GerenateUserId(int baseId, int maxId)
        {
            return random.Next(baseId, maxId);
        }

        static string GerenateUserName(long id)
        {
            var name = id.ToString().ToCharArray();
            for (var k = 0; k < name.Length; k++)
            {
                name[k] = (char)('a' + name[k] - '0');
            }

            return new string(name);
        }

        static void WriteTopic(List<Uri> brokersUriList, string topic, List<Message> messages)
        {
            var beginTime = DateTime.Now;
            var connectedTime = beginTime;
            using (var router = new BrokerRouter(new KafkaOptions(brokersUriList.ToArray())))
            using (var producer = new Producer(router))
            {
                connectedTime = DateTime.Now;
                producer.SendMessageAsync(topic, messages).Wait();
            }

            var endTime = DateTime.Now;
            Logger.InfoFormat("Wrote {0} messages into topic {1} , used time = {2}, connection used = {3}",
                messages.Count, topic, endTime - beginTime, connectedTime - beginTime);

            Logger.Info($"You can read it by : {ExePath} -{nameof(ArgReadWriteKafka.BrokerList)} {string.Join(",", brokersUriList)} " +
                $"-{nameof(ArgReadWriteKafka.ReadTopic)} {topic} ");
        }

        static void WriteTestData(List<Uri> brokersUriList, ArgReadWriteKafka options)
        {
            var baseId = 100;
            var maxId = baseId + (int)(options.Rows * 0.6);
            var idList = new HashSet<long>();
            for (var k = 0; k < maxId - baseId; k++)
            {
                idList.Add(GerenateUserId(baseId, maxId));
            }

            var tableIdUser = new List<Message>();
            foreach (var id in idList)
            {
                tableIdUser.Add(new Message(new RowIdUser { Id = id, User = GerenateUserName(id) }.ToString()));
            }

            Logger.Debug($"baseId = {baseId}, maxId = {maxId}, idList.count = {idList.Count}, to write rows = {options.Rows}");
            var tableIdCount = new List<Message>();
            for (var k = 0; k < options.Rows; k++)
            {
                var time = DateTime.Now.Add(TimeSpan.FromSeconds(k));
                tableIdCount.Add(new Message(new RowIdCountTime { Id = idList.ElementAt(random.Next(0, idList.Count - 1)), Count = 1, Time = time }.ToString()));
            }

            WriteTopic(brokersUriList, options.TopicIdUser, tableIdUser);

            WriteTopic(brokersUriList, options.TopicIdCount, tableIdCount);
        }

        static void ReadData(List<Uri> brokersUriList, ArgReadWriteKafka options)
        {
            if (string.IsNullOrWhiteSpace(options.ReadTopic))
            {
                Logger.WarnFormat($"{nameof(options.ReadTopic)} is null or empty! If you want to write, please set {nameof(options.IsWrite)} true.");
                return;
            }

            var beginTime = DateTime.Now;
            var connectedTime = beginTime;
            using (var router = new BrokerRouter(new KafkaOptions(brokersUriList.ToArray())))
            {
                using (var consumer = new Consumer(new ConsumerOptions(options.ReadTopic, router)))
                {
                    connectedTime = DateTime.Now;
                    var rows = 0;
                    foreach (var message in consumer.Consume())
                    {
                        rows++;
                        var text = Encoding.UTF8.GetString(message.Value);
                        if (options.ShowRowHeader)
                        {
                            Console.WriteLine("Row[{0}]: {1}", rows, text);
                        }
                        else
                        {
                            Console.WriteLine(text);
                        }

                        if (options.Rows > 0 && rows >= options.Rows)
                        {
                            break;
                        }
                    }

                    var endTime = DateTime.Now;
                    Logger.InfoFormat("Read {0} rows in topic {1}, brokers = {2}, used time = {3}, connection cost = {4}", rows, options.ReadTopic, options.BrokerList, endTime - beginTime, connectedTime - beginTime);
                }
            }
        }

        static void ListTopics(List<Uri> brokersUriList, ArgReadWriteKafka options)
        {

        }

        static void DeleteTopics(List<Uri> brokersUriList, ArgReadWriteKafka options, string topic)
        {

        }
    }
}
