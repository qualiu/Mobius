using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace testKeyValueStream
{
    public class ParserByCommandLine
    {
        private static IArgOptions Options = new ArgOptions();

        public static IArgOptions Parse(string[] args, out bool parseOK)
        {
            var parser = new CommandLine.Parser();
            parseOK = parser.ParseArguments(args, Options);
            //Log(string.Format("Parsed options = {0}", parser.ToString()));
            return Options;
        }

        [Serializable]
        class ArgOptions : IArgOptions
        {
            [Option('H', "host", DefaultValue = "127.0.0.1", HelpText = "host")]
            public string Host { get; set; }

            [Option('p', "port", DefaultValue = 9111, Required = true, HelpText = "port")]
            public int Port { get; set; }

            [Option('b', "batchSeconds", DefaultValue = 1, HelpText = "batch seconds")]
            public int BatchSeconds { get; set; }

            [Option('w', "windowSeconds", DefaultValue = 4, HelpText = "window seconds")]
            public int WindowSeconds { get; set; }

            [Option('s', "slideSeconds", DefaultValue = 4, HelpText = "slide seconds")]
            public int SlideSeconds { get; set; }

            [Option('r', "runningSeconds", DefaultValue = 30, HelpText = "running seconds")]
            public int RunningSeconds { get; set; }

            [Option('t', "testTimes", DefaultValue = 1, HelpText = "test times")]
            public int TestTimes { get; set; }

            [Option('c', "checkPointDirectory", DefaultValue = "checkDir", HelpText = "check point directory")]
            public string CheckPointDirectory { get; set; } // = Path.Combine(Path.GetTempPath(), "checkDir")

            [Option('d', "deleteCheckDirectory", DefaultValue = false, HelpText = "delete check point directory at first")]
            public bool NeedDeleteCheckPointDirectory { get; set; }

            [Option('m', "methodName", DefaultValue = "reduceByKeyAndWindow", HelpText = "method name, such as reduceByKeyAndWindow")]
            public string MethodName { get; set; }

            [Option('a', "isArrayValue", DefaultValue = true, HelpText = "is value type array")]
            public bool IsArrayValue { get; set; }

            [Option('u', "isUnevenArray", DefaultValue = false, HelpText = "is uneven array value")]
            public bool IsUnevenArray { get; set; }

            [Option('e', "elementCount", DefaultValue = 0, HelpText = "element count in value array. test memory like 1024*1024*20") ]
            public long ElementCount { get; set; }

            [Option('f', "saveTxtDirectory", DefaultValue = "", HelpText = "save file directory, not save if empty.")]
            public string SaveTxtDirectory { get; set; }

            [Option('k', "checkArray", DefaultValue = true, HelpText = "check array before operation such as reduce.")]
            public bool CheckArray { get; set; }

            [Option('v', "validateCount", DefaultValue = -1, HelpText = "line count to validate with, ignore if < 0 ")]
            public Int64 ValidateCount { get; set; }

            [Option("verbose", DefaultValue = true, HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }


            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }
        
    }
}
