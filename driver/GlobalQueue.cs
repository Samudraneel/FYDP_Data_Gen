using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace driver
{
    public class GlobalQueue
    {
        public static ConcurrentQueue<byte> Queue = new ConcurrentQueue<byte>();
        public static ConcurrentQueue<byte> CalibrationQueue = new ConcurrentQueue<byte>();
        public static ConcurrentQueue<byte> MagCalibrationQueue = new ConcurrentQueue<byte>();

        public static int GetSize(ConcurrentQueue<byte> Q)
        {
            return Q.Count;
        }

        public static void Add(ConcurrentQueue<byte> Q, byte[] data)
        {
            foreach (var val in data)
            {
                Q.Enqueue(val);
            }
        }

        public static MotionData Get(ConcurrentQueue<byte> Q)
        {
            byte[] arr = new byte[20];
            byte delim = new byte();
            bool exit = false;

            while (!exit)
            {
                Q.TryDequeue(out delim);

                if (delim == 0xCD)
                {
                    Q.TryDequeue(out delim);

                    if (delim == 0xAB)
                    {
                        for (int i = 0; i < 20; ++i)
                        {
                            Q.TryDequeue(out arr[i]);
                        }
                        exit = true;
                    }
                }
            }

            GCHandle handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
            MotionData data = (MotionData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(MotionData));
            handle.Free();

            return data;
        }

        public static void PrintAll(MotionData data)
        {
            Console.WriteLine("");
            Console.WriteLine("Finger ID: " + data.finger_id);
            Console.WriteLine("Knuckle ID: " + data.knuckle_id);

            Console.WriteLine("xAcc: " + data.xAcc);
            Console.WriteLine("yAcc: " + data.yAcc);
            Console.WriteLine("zAcc: " + data.zAcc);

            Console.WriteLine("xGyro: " + data.xGyro);
            Console.WriteLine("yGyro: " + data.yGyro);
            Console.WriteLine("zGyro: " + data.zGyro);

            Console.WriteLine("xMag: " + data.xMag);
            Console.WriteLine("yMag: " + data.yMag);
            Console.WriteLine("zMag: " + data.zMag);

            Console.ReadKey();
        }

        public static void Clear()
        {
            foreach (var q in Queue)
            {
                // do nothing
            }
        }
    }
}
