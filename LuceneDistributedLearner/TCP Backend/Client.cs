using System;
using System.Net;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;

namespace LuceneDistributedLearner.TCP_Backend
{
    public class Client
    {
        private string server;
        private Int32 port;
        private TcpClient client;
        private NetworkStream stream;
        private BinaryFormatter formatter;

        public Client(String server, Int32 port)
        {
            this.server = server;
            this.port = port;
            this.client = new TcpClient(server, port);
            this.stream = client.GetStream();
            this.formatter = new BinaryFormatter();
        }

        public void sendMessage(String message)
        {
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);

            Console.WriteLine("Sent: {0}", message);

            // Receive the TcpServer.response.

            // Buffer to store the response bytes.
            data = new Byte[256];

            // String to store the response ASCII representation.
            String responseData = String.Empty;

            // Read the first batch of the TcpServer response bytes.
            Int32 bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            Console.WriteLine("Received: {0}", responseData);
        }

        public void sendMessage(Object obj)
        {
            Byte[] data;

            using (MemoryStream ms = new MemoryStream())
            {
                this.formatter.Serialize(ms, obj);
                data = ms.ToArray();
            }

            stream.Write(data, 0, data.Length);

            Console.WriteLine("[CLIENT] Sent object");

            // Buffer to store the response bytes.
            data = new Byte[256];

            // String to store the response ASCII representation.
            String responseData = String.Empty;

            // Read the first batch of the TcpServer response bytes.
            Int32 bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            Console.WriteLine("Received: {0}", responseData);
        }

        public void stop()
        {
            // Close everything.
            Console.WriteLine("[CLIENT] Closing client");
            this.stream.Close();
            Console.WriteLine("1");
            this.client.Close();
            Console.WriteLine("2");
        }

        public void sendHeader(String header)
        {

        }

        public void sendObject()
        {
            
        }


        #region codigo_antigo
        //    public void Connect(String server, String message)
        //    {
        //        try
        //        {
        //            // Create a TcpClient.
        //            // Note, for this client to work you need to have a TcpServer 
        //            // connected to the same address as specified by the server, port
        //            // combination.
        //            Int32 port = 13000;
        //            TcpClient client = new TcpClient(server, port);

        //            // Translate the passed message into ASCII and store it as a Byte array.
        //            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

        //            // Get a client stream for reading and writing.
        //            //  Stream stream = client.GetStream();

        //            NetworkStream stream = client.GetStream();

        //            // Send the message to the connected TcpServer. 
        //            stream.Write(data, 0, data.Length);

        //            Console.WriteLine("Sent: {0}", message);

        //            // Receive the TcpServer.response.

        //            // Buffer to store the response bytes.
        //            data = new Byte[256];

        //            // String to store the response ASCII representation.
        //            String responseData = String.Empty;

        //            // Read the first batch of the TcpServer response bytes.
        //            Int32 bytes = stream.Read(data, 0, data.Length);
        //            responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
        //            Console.WriteLine("Received: {0}", responseData);

        //            // Close everything.
        //            stream.Close();
        //            client.Close();
        //        }
        //        catch (ArgumentNullException e)
        //        {
        //            Console.WriteLine("ArgumentNullException: {0}", e);
        //        }
        //        catch (SocketException e)
        //        {
        //            Console.WriteLine("SocketException: {0}", e);
        //        }

        //        //Console.WriteLine("\n Press Enter to continue...");
        //        //Console.Read();
        //    }
        //}

        #endregion
    }
}
