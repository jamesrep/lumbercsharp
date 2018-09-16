using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LumberjackTemplate
{
    class Program
    {
        static void Main(string[] args)
        {
            LumberjackTemplate lumber = new LumberjackTemplate();

            TcpServer tcpServer = new TcpServer();
            tcpServer.parseStream = new TcpServer.CALLBACK_parseStream(lumber.parseStream);
            lumber.setDataCallback(new LumberjackTemplate.CALLBACK_onDataFrame(frameReceived));

            tcpServer.listen();
        }

        private static void frameReceived(LumberjackTemplate.LumberFrame frame)
        {
            Console.WriteLine("frame: " + frame.sequenceNumber);
            Console.WriteLine(frame.strJson);
        }
    }
}
