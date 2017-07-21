using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;


namespace LuceneDistributedLearner.TCP_Backend
{
    public class Listener
    {
        private string address;
        private Int32 port;
        private TcpListener server;
        private static Int64 clientId = 0;
        private Thread listenLoopThread;
        private bool listening = true;
        BinaryFormatter formatter;
        private ConcurrentQueue<Object> dataQueue;
        private ConcurrentQueue<Object> answerQueue;
        private ManualResetEvent resetEvent;

        public Listener(string address, Int32 port, ConcurrentQueue<Object> dataQueue, ManualResetEvent resetEvent, ConcurrentQueue<Object> answerQueue)
        {          
            this.port = port;
            this.address = address;
            this.server = null;
            this.formatter = new BinaryFormatter();
            this.dataQueue = dataQueue;
            this.resetEvent = resetEvent;
            this.answerQueue = answerQueue;
        }

        public Thread start()
        {

            IPAddress localAddr = IPAddress.Parse(this.address);

            // TcpListener server = new TcpListener(port);
            server = new TcpListener(localAddr, this.port);

            // Start listening for client requests.
            server.Start();

            this.listenLoopThread = new Thread(() => listenLoop());
            this.listenLoopThread.Start();

            return listenLoopThread;
        }

        private void listenLoop()
        {
            try
            {
                // Enter the listening loop.
                while (this.listening)
                {
                    //Console.WriteLine("[SERVER] Waiting for connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    if (server.Pending())
                    {
                        TcpClient client = server.AcceptTcpClient();

                        Thread clientThread = new Thread(() => manageClient(client, clientId));
                        clientId++;
                        clientThread.Start();
                        Console.WriteLine("[SERVER] Thread " + (clientId - 1).ToString() + " connected!");
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("[SERVER] SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                this.stop();
            }
        } 

        public void stop()
        {
            Console.WriteLine("[SERVER] Closing server");
            this.listening = false;
            this.server.Stop();
        }

        private void manageClient(object tcpClient, Int64 myId)
        {
            TcpClient client = (TcpClient)tcpClient;
            
            // Buffer for reading data
            Byte[] bytes = new Byte[2560000];
            object data = null;

            // Get a stream object for reading and writing
            NetworkStream stream = client.GetStream();

            bool continueWhile = true;

            while (continueWhile)
            {
                //object my_out;
                //// Quando tiver mensagens na fila de "pronto", envia de volta para
                //// o Client (IndexManager)
                //if(answerQueue.TryDequeue(out my_out))
                //{
                //    Byte[] data_out;

                //    using (MemoryStream ms = new MemoryStream())
                //    {
                //        this.formatter.Serialize(ms, my_out);
                //        data_out = ms.ToArray();
                //    }

                //    stream.Write(data_out, 0, data_out.Length);
                //}    

                int i;
                // Loop to receive all the data sent by the client.
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string.
                    //data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ms.Write(bytes, 0, bytes.Length);
                        ms.Seek(0, SeekOrigin.Begin);
                        data = this.formatter.Deserialize(ms);
                    }

                    // Coloca dados em dataQueue para poderem ser acessados pelos processos
                    this.dataQueue.Enqueue(data);
                    resetEvent.Set();
                }
            }
            // Shutdown and end connection
            client.Close();
            Console.WriteLine("[SERVER] Closing Thread: " + myId.ToString());
        }
    }
}
