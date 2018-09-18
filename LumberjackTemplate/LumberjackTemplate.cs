// James Dickson 2018, example implementation of the Lumberjack-protocol 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.IO.Compression;

namespace LumberjackTemplate
{
    /**
    
    V1 Protocol description (sort of): https://github.com/elastic/logstash-forwarder/blob/master/PROTOCOL.md
    V2 ... no documentation found ... thus look at: https://github.com/elastic/go-lumber/blob/master/server/v2/reader.go

     */
    public class LumberjackTemplate
    {
        public delegate void CALLBACK_onDataFrame(LumberFrame frame);

        CALLBACK_onDataFrame onDataFrame = null;
        bool bContinue = true;
        System.Text.Encoding encoder = new  System.Text.ASCIIEncoding();
        uint windowSize = 0;
        byte[] ackBytes = new byte[6];
        uint lastFrameToAck = 0;
        uint frameReceivedCount = 0;


        public class LumberFrame
        {
            public int version;
            public int frameType;

            public uint sequenceNumber;
            public int pairCount;
            public uint keyLength;
            public uint valueLength;
            public string strJson;
        }



        public void setDataCallback(CALLBACK_onDataFrame cb)
        {
            this.onDataFrame = cb;
        }

        
        // Send ack for the received sequence-number (bulk-ack)
        void sendAck(Stream sr, uint sequenceNumber)
        {
            byte[] bts = BitConverter.GetBytes(sequenceNumber);
            ackBytes[0] = 0x32;
            ackBytes[1] = 0x41;
            Array.Copy(bts, 0, ackBytes, 2, 4);
            Array.Reverse(ackBytes, 2, 4);

            sr.Write(ackBytes, 0, ackBytes.Length);
            sr.Flush();
        }

        // Parse
        public void parseStream(Stream sr)
        {
            while (bContinue)
            {
                LumberFrame frame = new LumberFrame();

                frame.version = sr.ReadByte();

                Console.Write(((char)frame.version) + ":" + frame.version.ToString("X2"));

                if (frame.version < 0x31)
                {
                    throw new NotImplementedException("Frame version not supported");
                
                }

                frame.frameType = sr.ReadByte(); // Determine type of frame

                if (frame.frameType == 0x57)
                {

                    byte[] btsWindowSize = new byte[4];
                    int readBytes = sr.Read(btsWindowSize, 0, btsWindowSize.Length);

                    if (readBytes != btsWindowSize.Length) //error
                    {
                        sendAck(sr, lastFrameToAck); // We ack the last frame we received then crash the connection
                        return;
                    }

                    Array.Reverse(btsWindowSize);
                    windowSize = BitConverter.ToUInt32(btsWindowSize, 0); // the number of frames before ack.
                    frameReceivedCount = 0; // reset the frame-received-counter

                    Console.WriteLine("window size: " + windowSize);

                    continue;
                }
                else if (frame.frameType == 0x4a)
                {
                    byte[] btsHeader = new byte[8];
                    int retval = sr.Read(btsHeader, 0, btsHeader.Length);

                    if (retval != btsHeader.Length) return;

                    Array.Reverse(btsHeader, 0, 4);
                    Array.Reverse(btsHeader, 4, 4);


                    frame.sequenceNumber = BitConverter.ToUInt32(btsHeader, 0);

                    uint payloadLength = (uint)BitConverter.ToInt32(btsHeader, 4);
                    byte[] btsAll = new byte[payloadLength];

                    retval = sr.Read(btsAll, 0, (int)payloadLength);

                    frame.strJson = encoder.GetString(btsAll);

                    if (onDataFrame != null)
                    {
                        onDataFrame(frame);
                    }

                    lastFrameToAck = frame.sequenceNumber;
                    frameReceivedCount++;

                    if (frameReceivedCount >= windowSize)
                    {
                        sendAck(sr, lastFrameToAck);
                        frameReceivedCount = 0;

                        Console.WriteLine("ack sent");
                    }
                }
                else
                {
                    throw new NotImplementedException("Frame type not supported");
                }


            }
        }
    }
}
