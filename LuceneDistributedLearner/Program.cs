using System;
using CommandLine;
using CommandLine.Text;
using LuceneDistributedLearner;

namespace LuceneDistributedLearner
{
    class Options
    {
        [Option('r', "raw-data", DefaultValue = false, 
            HelpText = "Initialize a Raw Data Processor instance.")]
        public bool isRawDataProcessor { get; set; }

        [Option('i', "index-manager", DefaultValue=false, 
            HelpText = "Initialize an Index Manager instance.")]
        public bool isIndexManager { get; set; }

        [Option('v', "verbose", DefaultValue = false,
          HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        //--help to use it
        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                if (!(options.isIndexManager ^ options.isRawDataProcessor))
                {
                    Console.WriteLine("You must select one and only one of the options.");
                    return;
                }
                else
                {
                    //TODO: colocar como argumento
                    Console.WriteLine("-----");
                    TCP_Backend.Listener listener = new TCP_Backend.Listener("192.168.1.102", (Int32)13000);
                    listener.start();
                    Console.WriteLine("-----");
                    TCP_Backend.Client.Connect("192.168.1.102", "OLAR");
                    TCP_Backend.Client.Connect("192.168.1.102", "OLAR2");
                    TCP_Backend.Client.Connect("192.168.1.102", "OLAR3");
                    TCP_Backend.Client.Connect("192.168.1.102", "OLAR4");
                    TCP_Backend.Client.Connect("192.168.1.102", "OLAR5");
                    TCP_Backend.Client.Connect("192.168.1.102", "OLAR6");
                    //System.Threading.Thread.Sleep(10);
                    //listener.stop();
                }
                
            }
        }
    }
}
