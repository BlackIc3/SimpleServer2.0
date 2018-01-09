using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net;

namespace SimpleServer
{
    class Program
    {
        private static TcpListener listener;
        private static List<ServerThread> threads;
        private static bool stopThread;
        private static int port;
        private static IPAddress ip;
        public static bool log, loopHandle;

        static void Main(string[] args)
        {
            //Kleine Änderung, noch eine
            //Starten///////////////////////////////////////////////////////////////////////////////////
            ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            Console.Write("Port: ");
            port = int.Parse(Console.ReadLine());
            Console.Clear();

            listener = new TcpListener(ip, port);
            threads = new List<ServerThread>();
            listener.Start();
            Thread th = new Thread(new ThreadStart(Run));
            th.Start();
            Console.WriteLine("Server started and running at " + ip.ToString() + " on Port " + port.ToString());
            Console.WriteLine("-------------------------------------------------------------------");

            //Befehle handeln///////////////////////////////////////////////////////////////////////
            String cmd = "";
            loopHandle = true;
            while (loopHandle)
            {
                cmd = Console.ReadLine();
                if (cmd.ToLower() == "stop")
                {
                    Console.Clear();
                    loopHandle = false;
                }
                else if (cmd.ToLower() == "log")
                {
                    log = !log;
                    if (log)
                        Console.WriteLine("Logging on");
                    else
                        Console.WriteLine("Logging off");
                }
                else if (cmd.ToLower() == "cls")
                {
                    Console.Clear();
                    Console.WriteLine("Server started and running at " + ip.ToString() + " on Port " + port.ToString());
                    Console.WriteLine("-------------------------------------------------------------------");
                }
            }

            //Beenden//////////////////////////////////////////////////////////////////////////////////////
            broadcast("Shutting down the Server!$");
            Console.WriteLine("Stopping main thread...");
            stopThread = true;
            Console.WriteLine("Main thread stopped!");
            Console.WriteLine("Stopping running threads...");
            for (int i = 0; i < threads.Count; i++)
            {
                threads[i].stop = true;
                while (threads[i].running)
                    Thread.Sleep(1000);
                Console.WriteLine("Thread " + (i + 1).ToString() + " stopped");
            }
            Console.WriteLine("All threads stopped!");
            Console.WriteLine("Stopping TcpListener...");
            listener.Stop();
            Console.WriteLine("TcpListener stopped! \nAll services stopped! Program is safe to quit now!");
            Console.ReadKey();
        }

        public static void Run()
        {
            int counter = 0;
            while (!stopThread)
            {
                try
                {
                    TcpClient c = listener.AcceptTcpClient();             
                    threads.Add(new ServerThread(c, counter));
                }
                catch { }
                counter++;
            }
        }

        public static void broadcast(string Message)
        {
            for (int i = 0; i < threads.Count; i++)
            {
                threads[i].send(Message);
            }
        }
    }

    class ServerThread
    {
        public bool stop = false;
        public bool running = false;
        private TcpClient connection;
        public string userName;
        private int threadNo;

        public ServerThread(TcpClient c, int index)
        {
            threadNo = index;
            connection = c;
            new Thread(new ThreadStart(Run)).Start();
        }

        public void send(string msg)
        {
            Stream stream = connection.GetStream();
            Byte[] sendBytes = Encoding.UTF8.GetBytes(msg);
            stream.Write(sendBytes, 0, sendBytes.Length);
        }

        public void Run()
        {
            Byte[] bytesFrom = new Byte[65536];
            string input;
            Stream stream = connection.GetStream();
            stream.Read(bytesFrom, 0, (int)connection.ReceiveBufferSize);
            userName = Encoding.UTF8.GetString(bytesFrom);
            userName = userName.Substring(0, userName.IndexOf('$'));
            Console.WriteLine(userName + " joined");
            Program.broadcast(userName + " joined$");
            bool loop = true;
            while (loop)
            {
                try
                {
                    stream.Read(bytesFrom, 0, (int)connection.ReceiveBufferSize);
                    input = Encoding.UTF8.GetString(bytesFrom);
                    input = input.Substring(0, input.IndexOf('$'));
                    Program.broadcast(userName + " >> " + input + "$");
                    if (Program.log)
                        Console.WriteLine(userName + " >> " + input);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                }
            }
        }
    }
}
