using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

namespace testKeyValueStream
{
    public class ParserByPowerArgs : BaseUtil<ParserByPowerArgs>
    {
        private static IArgOptions Options = new ArgOptions();

        public static IArgOptions Parse(string[] args, out bool parseOK)
        {
            parseOK = false;
            if (args.Length < 1)
            {
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<ArgOptions>());
                return Options;
            }

            try
            {
                Options = Args.Parse<ArgOptions>(args);
                if (Options != null)
                {
                    parseOK = true;
                    Options.OutArgs((name, value) => Log(string.Format("{0} = {1}", name, value)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return Options;
        }

        [Serializable]
        [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
        public class ArgOptions : IArgOptions
        {
            [ArgShortcut("H"), ArgDefaultValue("127.0.0.1"), ArgDescription("host"), ArgRegex(@"^[\d\.]+$")]
            public string Host { get; set; }

            //[ArgShortcut("p"), ArgRequired(PromptIfMissing = true), ArgDefaultValue(9111), ArgDescription("port")]
            [ArgShortcut("p"), ArgRequired, ArgDefaultValue(9111), ArgDescription("port")]
            public int Port { get; set; }
            
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
            public bool NeedDeleteCheckPointDirectory { get; set; }

            [ArgShortcut("m"), ArgDefaultValue("reduceByKeyAndWindow"), ArgDescription("method name, such as reduceByKeyAndWindow")]
            public string MethodName { get; set; }

            [ArgShortcut("a"), ArgDefaultValue(true), ArgDescription("is value type array")]
            public bool IsArrayValue { get; set; }

            [ArgShortcut("u"), ArgDefaultValue(false), ArgDescription("is uneven array value")]
            public bool IsUnevenArray { get; set; }

            [ArgShortcut("e"), ArgDefaultValue(1024 * 1024 * 20), ArgDescription("element count in value array")]
            public long ElementCount { get; set; }

            [ArgShortcut("f"), ArgDefaultValue(""), ArgDescription("save file directory, not save if empty.")]
            public string SaveTxtDirectory { get; set; } // Path.Combine(Path.GetTempPath(), "checkDir")

            [ArgShortcut("k"), ArgDefaultValue(true), ArgDescription("check array before operation such as reduce.")]
            public bool CheckArray { get; set; }

            [ArgShortcut("v"), ArgDefaultValue(-1), ArgDescription("line count to validate with, ignore if < 0 ")]
            public Int64 ValidateCount { get; set; }

            [HelpHook, ArgDescription("Shows this help"), ArgShortcut("-?")]
            public bool Help { get; set; }

            
        }
    }
}
