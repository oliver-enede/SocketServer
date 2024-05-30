using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketServer
{
    class Program
    {
        static List<Socket> clientSockets = new List<Socket>();
        static object lockObj = new object();

        static void Main(string[] args)
        {
            Socket listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipaddr = IPAddress.Any;
            IPEndPoint ipep = new IPEndPoint(ipaddr, 25000);

            try
            {
                listenerSocket.Bind(ipep);
                listenerSocket.Listen(5);

                Console.WriteLine("Server is listening on port 25000...");

                while (true)
                {
                    Socket clientSocket = listenerSocket.Accept();
                    lock (lockObj)
                    {
                        clientSockets.Add(clientSocket);
                    }
                    Console.WriteLine("Client connected: " + clientSocket.RemoteEndPoint.ToString());

                    Thread clientThread = new Thread(HandleClient);
                    clientThread.Start(clientSocket);
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }
        }

        static void HandleClient(object obj)
        {
            Socket clientSocket = (Socket)obj;
            byte[] buff = new byte[128];

            try
            {
                while (true)
                {
                    int numberOfReceivedBytes = clientSocket.Receive(buff);
                    if (numberOfReceivedBytes == 0) break;

                    string receivedText = Encoding.ASCII.GetString(buff, 0, numberOfReceivedBytes);
                    Console.WriteLine("Data sent by client is: " + receivedText);

                    Broadcast(receivedText, clientSocket);

                    if (receivedText.Equals("<EXIT>", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    Array.Clear(buff, 0, buff.Length);
                }
            }
            catch (Exception)
            {
                // Handle any exceptions
            }
            finally
            {
                lock (lockObj)
                {
                    clientSockets.Remove(clientSocket);
                }
                clientSocket.Close();
                Console.WriteLine("Client disconnected: " + clientSocket.RemoteEndPoint.ToString());
            }
        }

        static void Broadcast(string message, Socket excludeSocket)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);

            lock (lockObj)
            {
                foreach (var client in clientSockets)
                {
                    if (client != excludeSocket)
                    {
                        client.Send(data);
                    }
                }
            }
        }
    }
}