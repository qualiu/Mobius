using System;
using PowerArgs;

namespace SourceLinesSocket
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

            [ArgShortcut("s"), ArgRange(0, 999999), ArgDefaultValue(100), ArgDescription("send interval by milliseconds")]
            public int SendInterval { get; set; }

            [ArgShortcut("r"), ArgDefaultValue(3600), ArgDescription("running duration by seconds")]
            public int RunningSeconds { get; set; }

            [ArgShortcut("n"), ArgDefaultValue(0), ArgDescription("messages per connection : 0 -> no limit")]
            public int MessagesPerConnection { get; set; }

            [ArgShortcut("q"), ArgDefaultValue(true), ArgDescription("quit if exceeded running duration or sent message count")]
            public bool QuitIfExceededAny { get; set; }

            [ArgShortcut("x"), ArgDefaultValue(0), ArgDescription("max connect times : 0 -> no limit")]
            public int MaxConnectTimes { get; set; }

            [ArgShortcut("z"), ArgDefaultValue(0), ArgDescription("pause seconds at each connection lost : 0 -> no pause")]
            public int PauseSecondsAtDrop { get; set; }

            [HelpHook, ArgDescription("Shows this help"), ArgShortcut("-?")]
            public bool Help { get; set; }
        }
    }
}
