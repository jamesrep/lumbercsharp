using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LumberjackTemplate
{
    class TcpServer
    {
        public delegate void CALLBACK_parseStream(Stream stream);

        TcpListener listener = null;
        bool done = false;
        string strHost = "0.0.0.0";
        private int listenPort = 5044;

        bool bContinue = true;
        public CALLBACK_parseStream parseStream = null;


        public void closeConnections()
        {
            done = true;
        }

        public class ClientHandler
        {
            public TcpClient client = null;
            public IPEndPoint groupEP = null;
            public CALLBACK_parseStream parseStream = null;


            public void startThread()
            {

                System.Threading.Thread th = new System.Threading.Thread(new System.Threading.ThreadStart(this.handleCommunication));
                th.Start();
            }

            public virtual void handleCommunication()
            {
                try
                {
                    NetworkStream stream = client.GetStream();

                    parseStream(stream);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void listen()
        {
            done = false;
            bContinue = true;

            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            IPAddress localAddr = IPAddress.Parse(strHost);
            listener = new TcpListener(localAddr, listenPort);
            listener.Start();

            try
            {
                while (!done && bContinue)
                {

                    TcpClient client = listener.AcceptTcpClient();
                    ClientHandler ch = new ClientHandler();
                    ch.parseStream = this.parseStream;
                    ch.client = client;
                    ch.groupEP = groupEP;
                    ch.startThread();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


        }
    }
}
