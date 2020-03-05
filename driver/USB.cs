using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Management;
using System.Linq;
using System.IO.Ports;
using System.Text;
using System.Collections.Concurrent;

namespace driver
{
    [Serializable]
    public struct FeedbackStruct
    {
        // member variables for struct
    }

    public struct MotionData
    {
        public byte finger_id;
        public byte knuckle_id;

        public short xAcc;
        public short yAcc;
        public short zAcc;

        public short xGyro;
        public short yGyro;
        public short zGyro;

        public short xMag;
        public short yMag;
        public short zMag;
    }

    public class USB
    {
        public static SerialPort _serialPort;
        private static readonly int BYTES_TO_READ = 375;
        public Boolean READ = true;

        public USB() { }

        public SerialPort Open(int baudRate, int readTimeout, int writeTimeout)
        {
            _serialPort = new SerialPort();

            _serialPort.PortName = "COM4";
            _serialPort.BaudRate = baudRate;
            _serialPort.ReadTimeout = readTimeout;
            _serialPort.WriteTimeout = writeTimeout;

            _serialPort.Open();

            return _serialPort;
        }

        public void Read(ConcurrentQueue<byte> Q)
        {
            //Console.WriteLine("i am fucking insane");

            byte[] bytes = new byte[BYTES_TO_READ];
            _serialPort.Read(bytes, 0, BYTES_TO_READ);

            if (true)
            {
                GlobalQueue.Add(Q, bytes);
            }
        }

        public void Close()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        public void End()
        {
            READ = false;
        }
    }
}
