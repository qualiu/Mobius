using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceLinesSocket
{
    public class ParserByFluent : BaseUtil<ParserByFluent>
    {
        internal static IArgOptions Options = new ArgOptions();

        public static IArgOptions Parse(string[] args, out bool parseOK)
        {
            var parser = new Fclp.FluentCommandLineParser<ArgOptions>();
            parser.Setup(arg => arg.Host).As('H', "host").SetDefault("127.0.0.1").WithDescription("host").Callback(arg => { Options.Host = arg; Log("host = " + arg); });
            parser.Setup(arg => arg.Port).As('p', "port").Required().SetDefault(9111).WithDescription("port").Callback(arg => { Options.Port = arg; Log("port = " + arg); });
            parser.Setup(arg => arg.SendInterval).As('s', "sendInterval").SetDefault(100).WithDescription("interval milliseconds").Callback(arg => { Options.SendInterval = arg; Log("send interval = " + arg + " ms"); });
            parser.Setup(arg => arg.RunningSeconds).As('r', "runningSeconds").SetDefault(3600).WithDescription("running seconds").Callback(arg => { Options.RunningSeconds = arg; Log("running seconds = " + arg + " s"); });
            parser.Setup(arg => arg.MessagesPerConnection).As('n', "messagesPerConnection").SetDefault(0).WithDescription("send message count per connection. 0 = no limit").Callback(arg => { Options.MessagesPerConnection = arg; Log("messages per connection = " + arg); });
            parser.Setup(arg => arg.KeysPerConnection).As('n', "keysPerConnection").SetDefault(0).WithDescription("send message count per connection. 0 = no limit").Callback(arg => { Options.KeysPerConnection = arg; Log("keys per connection = " + arg); });
            parser.Setup(arg => arg.QuitIfExceededAny).As('q', "quitIfExceeded").SetDefault(true).WithDescription("quit if exceed time or message-count").Callback(arg => { Options.QuitIfExceededAny = arg; Log("quit if exceeded any condition = " + arg); });
            parser.Setup(arg => arg.MaxConnectTimes).As('q', "maxConnectTimes").SetDefault(0).WithDescription("quit if exceed time or message-count").Callback(arg => { Options.MaxConnectTimes = arg; Log("quit if exceeded any condition = " + arg); });
            parser.Setup(arg => arg.PauseSecondsAtDrop).As('q', "pause seconds at each connection lost").SetDefault(0).WithDescription("pause seconds at each connection lost").Callback(arg => { Options.PauseSecondsAtDrop = arg; Log("pause seconds at each connection lost = " + arg); });
            parser.SetupHelp("h", "help").Callback(text => Console.WriteLine(text));
            var result = parser.Parse(args);

            //Log(string.Format("ParserFluentArgs : p = {0}", p.ToString()));
            if (result.HasErrors || args.Length < 1)
            {
                parser.HelpOption.ShowHelp(parser.Options);
                parseOK = false;
            }

            parseOK = true;
            return Options;
        }

        [Serializable]
        public class ArgOptions : IArgOptions
        {
            public string Host { get; set; }
            public int Port { get; set; }
            public int SendInterval { get; set; }
            public int RunningSeconds { get; set; }
            public int MessagesPerConnection { get; set; }
            public int KeysPerConnection { get; set; }
            public bool QuitIfExceededAny { get; set; }
            public int MaxConnectTimes { get; set; }
            public int PauseSecondsAtDrop { get; set; }
        }

    }
}
