using System;
using CommonTestUtils;
using PowerArgs;

namespace kafkaStreamTest
{
    [Serializable]
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public class ArgOptions
    {
        [ArgDefaultValue("localhost:9092"), ArgDescription("Kafka metadata.broker.list")]
        public String brokerList { get; set; }


        [ArgDefaultValue("localhost:2181"), ArgDescription("Zookeeper connection string")]
        public String zookeeper { get; set; }


        [ArgDefaultValue("smallest"), ArgDescription("auto.offset.reset")]
        public String autoOffset { get; set; }


        [ArgDefaultValue("lzKafkaTestGroup"), ArgDescription("Kafka group id")]
        public String groupId { get; set; }


        [ArgDefaultValue("test"), ArgDescription("Kafka topic name"), ArgRequired()]
        public String topic { get; set; }


        [ArgDefaultValue(1), ArgDescription("Kafka topic partition")]
        public int partition { get; set; }


        [ArgDefaultValue(0), ArgDescription("Kafka topic fromOffset")]
        public long fromOffset { get; set; }


        [ArgDefaultValue(60), ArgDescription("Kafka topic untilOffset")]
        public long untilOffset { get; set; }


        [ArgShortcut("b"), ArgDescription("batch seconds"), ArgDefaultValue(1), ArgRange(1, 999)]
        public int BatchSeconds { get; set; }

        [ArgShortcut("w"), ArgDescription("window seconds"), ArgDefaultValue(4)]
        public int WindowSeconds { get; set; }

        [ArgShortcut("s"), ArgDescription("slide seconds"), ArgDefaultValue(4)]
        public int SlideSeconds { get; set; }

        [ArgShortcut("r"), ArgDescription("running seconds"), ArgDefaultValue(30)]
        public int RunningSeconds { get; set; }

        [ArgShortcut("t"), ArgDescription("test times"), ArgDefaultValue(1)]
        public int TestTimes { get; set; }

        [ArgShortcut("c"), ArgDefaultValue("checkDir"), ArgExample("checkDir", "check point directory")]
        public string CheckPointDirectory { get; set; } // = Path.Combine(Path.GetTempPath(), "checkDir")

        [ArgShortcut("d"), ArgDefaultValue(false), ArgDescription("delete check point directory at first")]
        public bool DeleteCheckPointDirectory { get; set; }

        [ArgShortcut("a"), ArgDefaultValue(true), ArgDescription("is value type array")]
        public bool IsArrayValue { get; set; }

        [ArgShortcut("u"), ArgDefaultValue(false), ArgDescription("is uneven array value")]
        public bool IsUnevenArray { get; set; }

        [ArgShortcut("e"), ArgDefaultValue(0), ArgDescription("element count in value array. 0 means not set.")]
        public long ElementCount { get; set; }

        [ArgShortcut("f"), ArgDefaultValue(""), ArgDescription("save file directory, not save if empty.")]
        public string SaveTxtDirectory { get; set; }

        [ArgShortcut("k"), ArgDefaultValue(true), ArgDescription("check array before operation such as reduce.")]
        public bool CheckArray { get; set; }

        [ArgShortcut("v"), ArgDefaultValue(-1), ArgDescription("line count to validate with, ignore if < 0 ")]
        public Int64 ValidateCount { get; set; }

        [HelpHook, ArgDescription("Shows this help"), ArgShortcut("-h")]
        public bool Help { get; set; }
    }
}
