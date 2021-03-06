﻿using System;
using System.Net;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using System.Collections.Generic;
using AutoComplete.Classes;
using System.Linq;

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

            Console.WriteLine("Sent message..."); //, message);

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

            List<DataType> x = ((IEnumerable)obj).Cast<DataType>().ToList();

            using (MemoryStream ms = new MemoryStream())
            {
                this.formatter.Serialize(ms, obj);
                data = ms.ToArray();
            }

            stream.Write(data, 0, data.Length);

            Console.WriteLine("[CLIENT] Sent object");
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
    }
}
