using System;
using CommandLine;
using CommandLine.Text;
using System.IO;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using LuceneDistributedLearner;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using AutoComplete.Classes;
using System.Collections.Generic;

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
                    while (LuceneProcessor.luceneIsBusy())
                        System.Threading.Thread.Sleep(1000);
                    Console.WriteLine("[INDEX_MANAGER] Merging data...");
                    if (!(LuceneProcessor.luceneIsBusy()))
                        LuceneProcessor.indexUpdateWord(((IEnumerable)currMessage).Cast<DataType>().ToList());
                }
                Console.WriteLine("[INDEX_MANAGER] Done!");
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
                    ManualResetEvent resetEvent = new ManualResetEvent(false); // Signalled state
                    ConcurrentQueue<Object> dataQueue = new ConcurrentQueue<Object>();
                    ConcurrentQueue<Object> answerQueue = new ConcurrentQueue<Object>();
                    LuceneProcessor.initializeSuggestor();
                    if (options.isRawDataProcessor)
                    {
                        TCP_Backend.Client client = new TCP_Backend.Client(options.processAddress, options.processPort);
                        TCP_Backend.Listener listener = new TCP_Backend.Listener("0.0.0.0", options.processPort,
                                                                               dataQueue, resetEvent, answerQueue);
                        listener.start();
                        Console.WriteLine("[RAW_DATA_PROCESSOR] Start...");
                        while (true)
                        {
                            // Bloqueia ate chegar uma mensagem
                            resetEvent.WaitOne();
                            Object currMessage;
                            int processed_count = 0;
                            while (dataQueue.TryDequeue(out currMessage))
                            {
                                processed_count++;
                                Console.WriteLine("[RAW_DATA_PROCESSOR] Reading text to process...");
                                Console.WriteLine((string)currMessage);
                                LuceneProcessor.indexText((string)currMessage);
                                while (LuceneProcessor.luceneIsBusy())
                                    System.Threading.Thread.Sleep(500);
                            }
                            if (!LuceneProcessor.luceneIsBusy()) 
                                client.sendMessage((object)LuceneProcessor.getAllIndexes());

                            Console.WriteLine("[RAW_DATA_PROCESSOR] Reading done!");
                            // Quando acabarem as mensagens bloqueia novamente
                            resetEvent.Reset();
                        }
                        Console.ReadLine();
                        Environment.Exit(0);
                        
                       
                    }
                    else if (options.isIndexManager)
                    {
                        TCP_Backend.Listener listener = new TCP_Backend.Listener("0.0.0.0", options.processPort,
                                                                               dataQueue, resetEvent, answerQueue);
                        listener.start();
                        System.Threading.Thread.Sleep(5000);
                        TCP_Backend.Client client = new TCP_Backend.Client(options.processAddress, options.processPort);

                        Thread monitorThread = new Thread(() => indexManagerMonitor(dataQueue, resetEvent));
                        monitorThread.Start();

                        List<DataType> raw_data_processed = new List<DataType>();
                        StreamReader reader = new StreamReader(@"C:\Users\Guilherme\Desktop\resumo_0630.txt");
                        string raw_text = reader.ReadToEnd();
                        string temp_text = "";
                        while (raw_text != "")
                        {
                            Object chunk_message;
                            try
                            {
                                int index = raw_text.IndexOf(' ', 500);
                                temp_text = raw_text.Substring(0, index); //copio aqui

                                raw_text = raw_text.Substring(index+1); //corto aqui
                                chunk_message = (Object)temp_text;
                            }
                            catch
                            {
                                chunk_message = (Object)raw_text;
                                raw_text = "";
                            }                            
                            client.sendMessage(chunk_message);
                            System.Threading.Thread.Sleep(1000);
                        }
                        Console.ReadLine();                        
                        Environment.Exit(0);
                    }
                    

                }
            }
        }
    }
}
