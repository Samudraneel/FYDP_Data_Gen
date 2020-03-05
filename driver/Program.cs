using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace driver
{
    class Program
    {
        private static USB glove;
        private static readonly int WAIT_TIMER = 5000;
        private static readonly int TOTAL_FINGER_REFS = 6;
        private static readonly int TOTAL_KNUCKLE_REFS = 3;

        static void Main(string[] args)
        {
            Console.WriteLine("in main");
            USBThread usb = new USBThread();
            Calibrator calibrate = new Calibrator();

            glove = new USB();
            glove.Open(1152000, 1000, 1000);

            string path = "C:\\Users\\sam_n\\Documents\\offsets.txt";

            Console.WriteLine("Everything is initialized!");

            _Calibrate();
            calibrate.CollectAccelGyroData();

            _MagCalibrate();
            calibrate.CollecMagData();

            calibrate.CalibrateOffsets();

            using (StreamWriter w = new StreamWriter(path))
            {
                Console.WriteLine("Entering writer.");

                for (int i = 0; i < TOTAL_FINGER_REFS; i++)
                {
                    if (i == (int)Finger.HAND)
                    {
                        w.Write("Finger_ID: " + i + " \n"
                              + "Knuckle_ID: " + 1 + " \n"
                              + "Accel_Offset_x_y_z: " + calibrate.accelOffset[i, 1].x + " " + calibrate.accelOffset[i, 1].y + " " + calibrate.accelOffset[i, 1].z + " \n"
                              + "Gyro_Offset_x_y_z: " + calibrate.gyroOffset[i, 1].x + " " + calibrate.gyroOffset[i, 1].y + " " + calibrate.gyroOffset[i, 1].z + " \n"
                              + "Mag_offset_x_y_z: " + calibrate.magOffset[i, 1].x + " " + calibrate.magOffset[i, 1].y + " " + calibrate.magOffset[i, 1].z + " \n");
                    }
                    else
                    {
                        for (int j = 0; j < TOTAL_KNUCKLE_REFS; j++)
                        {
                            w.Write("Finger_ID: " + i + " \n"
                              + "Knuckle_ID: " + j + " \n"
                              + "Accel_Offset_x_y_z: " + calibrate.accelOffset[i, j].x + " " + calibrate.accelOffset[i, j].y + " " + calibrate.accelOffset[i, j].z + " \n"
                              + "Gyro_Offset_x_y_z: " + calibrate.gyroOffset[i, j].x + " " + calibrate.gyroOffset[i, j].y + " " + calibrate.gyroOffset[i, j].z + " \n"
                              + "Mag_offset_x_y_z: " + calibrate.magOffset[i, j].x + " " + calibrate.magOffset[i, j].y + " " + calibrate.magOffset[i, j].z + " \n");
                        }
                    }
                }

                Console.WriteLine("Exiting writer.");
                Console.WriteLine("Done getting calibration data!");
            }

            bool end = false;
            string path2 = "C:\\Users\\sam_n\\Documents\\lmao.txt";

            Task check = new Task(() =>
            {
                using (StreamWriter w = new StreamWriter(path2))
                {
                    Console.WriteLine("entering loop");
                    while (GlobalQueue.GetSize(GlobalQueue.Queue) > 1000)
                    {
                        MotionData data = GlobalQueue.Get(GlobalQueue.Queue);
                        w.Write(data.finger_id + " " + data.knuckle_id + " "
                            + data.xAcc + " " + data.yAcc + " " + data.zAcc + " "
                            + data.xGyro + " " + data.yGyro + " " + data.zGyro + " "
                            + data.xMag + " " + data.yMag + " " + data.zMag + " " + "\n");
                    }
                    Console.WriteLine("done getting dummy data.");
                    glove.Close();
                    Console.ReadKey();
                }
            });

            Task endTask = new Task(() =>
            {
                Console.WriteLine("DO SOME ACTION");
                var startTime = DateTime.UtcNow;

                while (DateTime.UtcNow - startTime < TimeSpan.FromMilliseconds(2*WAIT_TIMER))
                {
                    glove.Read(GlobalQueue.Queue);
                }
            });

            endTask.Start();
            endTask.Wait();
            check.Start();
            check.Wait();
        }

        private static void _Calibrate()
        {
            Console.WriteLine("Entered _Calibrate() function!");
            Console.WriteLine("LEAVE IT ALONE!");

            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime < TimeSpan.FromMilliseconds(WAIT_TIMER))
            {
                glove.Read(GlobalQueue.CalibrationQueue);
            }

            Console.WriteLine("Filled up CalibrationQueue, will exit function.");
        }

        private static void _MagCalibrate()
        {
            Console.WriteLine("Entered _MagCalibrate() function!");
            Console.WriteLine("DO THE THING");

            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime < TimeSpan.FromMilliseconds(2 * WAIT_TIMER))
            {
                glove.Read(GlobalQueue.MagCalibrationQueue);
            }

            Console.WriteLine("Filled up MagCalibrationQueue, will exit function.");
        }


    }
}
