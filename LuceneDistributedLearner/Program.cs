using System;
using CommandLine;
using CommandLine.Text;
using LuceneDistributedLearner;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

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
                    //TODO: colocar como argumento o endereco
                    TCP_Backend.Listener listener = new TCP_Backend.Listener("192.168.1.102", (Int32)13000);
                    listener.start();
                    //System.Threading.Thread.Sleep(10000);
                    TCP_Backend.Client client = new TCP_Backend.Client("192.168.1.102", (Int32)13000);

                    //client.sendMessage("oi servidorzineo");
                    string tutui = "vai tomar no cu";
                    Object objetozineo = (Object)tutui; 
                    client.sendMessage(objetozineo);
                    System.Threading.Thread.Sleep(2000);
                    //for (int i = 0; i < 1000000; i+=2)
                    //    i-=1;
                    //client.sendMessage("Tutui gay");
                    //client.sendMessage("para");
                    client.stop();
                    listener.stop();

                    //System.Threading.Thread.Sleep(10);
                    //listener.stop();
                }

            }
        }
    }
}
