using System;
using CommandLine;
using CommandLine.Text;
using System.Collections.Concurrent;
using System.Threading;
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

        [Option('a', "address", Required = true,
            HelpText = "Address in which process will be initialized.")]
        public string processAddress { get; set; }

        [Option('p', "port", Required = true,
            HelpText = "Port in which process will be initialized.")]
        public Int32 processPort { get; set; }

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
        static private void indexManagerMonitor(ConcurrentQueue<Object> dataQueue, ManualResetEvent resetEvent)
        {
            while (true)
            {
                // Bloqueia ate chegar uma mensagem
                resetEvent.WaitOne();
                Object currMessage;

                while (dataQueue.TryDequeue(out currMessage))
                {
                    Console.WriteLine("[PROGRAM] Reading message : " + (string)currMessage);
                }

                // Quando acabarem as mensagens bloqueia novamente
                resetEvent.Reset();
            }
        }

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
                    ManualResetEvent resetEvent = new ManualResetEvent(true); // Signalled state
                    ConcurrentQueue<Object> dataQueue = new ConcurrentQueue<Object>();

                    Thread monitorThread = new Thread(() => indexManagerMonitor(dataQueue, resetEvent));
                    monitorThread.Start();

                    //TODO: colocar como argumento o endereco
                    TCP_Backend.Listener listener = new TCP_Backend.Listener(options.processAddress, options.processPort, 
                                                                             dataQueue, resetEvent);
                    listener.start();
                    //System.Threading.Thread.Sleep(10000);
                    TCP_Backend.Client client = new TCP_Backend.Client(options.processAddress, options.processPort);

                    TCP_Backend.Client client2 = new TCP_Backend.Client(options.processAddress, options.processPort);

                    //client.sendMessage("oi servidorzineo");
                    string tutui = "vai tomar no cu";
                    Object objetozineo = (Object)tutui; 
                    client.sendMessage(objetozineo);

                    string tutui2 = "vai tomar no cu2";
                    Object objetozineo2 = (Object)tutui2;
                    client2.sendMessage(objetozineo2);
                    client.sendMessage(objetozineo2);

                    client.stop();
                    client2.stop();
                    listener.stop();
                    Console.WriteLine("cabo");

                    Environment.Exit(0);
                }

            }
        }
    }
}
