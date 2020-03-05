using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace driver
{
    public enum Finger
    {
        PINKY = 0,
        RING = 1,
        MIDDLE = 2,
        INDEX = 3,
        THUMB = 4,
        HAND = 5,
    }

    /* Might not need */
    public enum Knuckle
    {
        BASE = 0,
        SECOND = 1,
        TIP = 2,
    }

    class Calibrator
    {
        private readonly double ACCEL_SENSITIVITY = 0.061;
        private readonly double GYRO_SENSITIVITY = 8.75;
        private readonly double MAG_SENSITIVITY = 0.14;
        private readonly float MULTIPLIER = 1.0f / 1000.0f;
        private readonly int TOTAL_FINGER_REFS = 6;
        private readonly int TOTAL_KNUCKLE_REFS = 3;
        public MotionData calibrationData;
        public Vector3[,] accel;
        public Vector3[,] gyro;
        public Vector3[,] maxMag;
        public Vector3[,] minMag;
        public Vector3[,] accelOffset;
        public Vector3[,] gyroOffset;
        public Vector3[,] magOffset;
        private int[,] numDataPoints;

        public Calibrator()
        {
            /* Initialize all calibration data to 0 at first */
            calibrationData = new MotionData();
            /* Initialize data arrays */
            accel = new Vector3[TOTAL_FINGER_REFS, TOTAL_KNUCKLE_REFS];
            maxMag = new Vector3[TOTAL_FINGER_REFS, TOTAL_KNUCKLE_REFS];
            minMag = new Vector3[TOTAL_FINGER_REFS, TOTAL_KNUCKLE_REFS];
            gyro = new Vector3[TOTAL_FINGER_REFS, TOTAL_KNUCKLE_REFS];
            /* Initialize offset arrays */
            accelOffset = new Vector3[TOTAL_FINGER_REFS, TOTAL_KNUCKLE_REFS];
            magOffset = new Vector3[TOTAL_FINGER_REFS, TOTAL_KNUCKLE_REFS];
            gyroOffset = new Vector3[TOTAL_FINGER_REFS, TOTAL_KNUCKLE_REFS];
            numDataPoints = new int[TOTAL_FINGER_REFS, TOTAL_KNUCKLE_REFS];

            for (int i = 0; i < TOTAL_FINGER_REFS; i++)
            {
                if (i == (int)Finger.HAND)
                {
                    accel[i, 1] = new Vector3();
                    maxMag[i, 1] = new Vector3();
                    minMag[i, 1] = new Vector3();
                    gyro[i, 1] = new Vector3();

                    accelOffset[i, 1] = new Vector3();
                    magOffset[i, 1] = new Vector3();
                    gyroOffset[i, 1] = new Vector3();

                    numDataPoints[i, 1] = 0;
                }
                else
                {
                    for (int j = 0; j < TOTAL_KNUCKLE_REFS; j++)
                    {
                        accel[i, j] = new Vector3();
                        maxMag[i, j] = new Vector3();
                        minMag[i, j] = new Vector3();
                        gyro[i, j] = new Vector3();

                        accelOffset[i, j] = new Vector3();
                        magOffset[i, j] = new Vector3();
                        gyroOffset[i, j] = new Vector3();

                        numDataPoints[i, j] = 0;
                    }
                }
            }
        }

        public void CollectAccelGyroData()
        {
            Console.WriteLine("Will collect AccelGyroData. Inside of CollectAccelGyroData().");
            int fingerId, knuckleId;

            Console.WriteLine(GlobalQueue.GetSize(GlobalQueue.CalibrationQueue));

            while (GlobalQueue.GetSize(GlobalQueue.CalibrationQueue) > 1000)
            {
                calibrationData = GlobalQueue.Get(GlobalQueue.CalibrationQueue);

                fingerId = (int)calibrationData.finger_id;
                knuckleId = (int)calibrationData.knuckle_id;

                /* Get acceleration data */
                accel[fingerId, knuckleId].x += calibrationData.xAcc;
                accel[fingerId, knuckleId].y += calibrationData.yAcc;
                accel[fingerId, knuckleId].z += calibrationData.zAcc;
                /* Get gyro data */
                gyro[fingerId, knuckleId].x += calibrationData.xGyro;
                gyro[fingerId, knuckleId].y += calibrationData.yGyro;
                gyro[fingerId, knuckleId].z += calibrationData.zGyro;
                /* Increment number of data points for this finger+knuckle */
                numDataPoints[fingerId, knuckleId] += 1;
            }

            Console.WriteLine("Exiting CollectAccelGyroData().");
        }

        public void CollecMagData()
        {
            Console.WriteLine("Will collect MagData. Inside of CollectMagData().");

            int fingerId, knuckleId;
            while (GlobalQueue.GetSize(GlobalQueue.MagCalibrationQueue) > 1000)
            {
                calibrationData = GlobalQueue.Get(GlobalQueue.MagCalibrationQueue);
                fingerId = (int)calibrationData.finger_id;
                knuckleId = (int)calibrationData.knuckle_id;

                /* Get mag data */
                setMaxMag(fingerId, knuckleId, calibrationData);
                setMinMag(fingerId, knuckleId, calibrationData);
            }

            Console.WriteLine("Exiting CollectMagData().");
        }

        public void CalibrateOffsets()
        {
            for (int i = 0; i < TOTAL_FINGER_REFS; i++)
            {
                if (i == (int)Finger.HAND)
                {
                    /* Set hand acceleration offsets */
                    accelOffset[i, 1].x = accel[i, 1].x / numDataPoints[i, 1];
                    accelOffset[i, 1].y = accel[i, 1].y / numDataPoints[i, 1];
                    accelOffset[i, 1].z = accel[i, 1].z / numDataPoints[i, 1];
                    /* Set hand gyro offsets */
                    gyroOffset[i, 1].x = gyro[i, 1].x / numDataPoints[i, 1];
                    gyroOffset[i, 1].y = gyro[i, 1].y / numDataPoints[i, 1];
                    gyroOffset[i, 1].z = gyro[i, 1].z / numDataPoints[i, 1];
                    /* Set hand mag offsets */
                    magOffset[i, 1].x = (maxMag[i, 1].x + minMag[i, 1].x) / 2;
                    magOffset[i, 1].y = (maxMag[i, 1].y + minMag[i, 1].y) / 2;
                    magOffset[i, 1].z = (maxMag[i, 1].z + minMag[i, 1].z) / 2;
                }
                else
                {
                    for (int j = 0; j < TOTAL_KNUCKLE_REFS; j++)
                    {
                        /* Set knuckle acceleration offsets */
                        accelOffset[i, j].x = accel[i, j].x / numDataPoints[i, j];
                        accelOffset[i, j].y = accel[i, j].y / numDataPoints[i, j];
                        accelOffset[i, j].z = accel[i, j].z / numDataPoints[i, j];
                        /* Set knuckle gyro offsets */
                        gyroOffset[i, j].x = gyro[i, j].x / numDataPoints[i, j];
                        gyroOffset[i, j].y = gyro[i, j].y / numDataPoints[i, j];
                        gyroOffset[i, j].z = gyro[i, j].z / numDataPoints[i, j];
                        /* Set knuckle gyro offsets */
                        magOffset[i, j].x = (maxMag[i, j].x + minMag[i, j].x) / 2;
                        magOffset[i, j].y = (maxMag[i, j].y + minMag[i, j].y) / 2;
                        magOffset[i, j].z = (maxMag[i, j].z + minMag[i, j].z) / 2;
                    }
                }
            }
            //ConvertOffsetUnits();
        }

        private void setMaxMag(int fingerId, int knuckleId, MotionData potentialMax)
        {
            if (maxMag[fingerId, knuckleId].x < potentialMax.xMag)
            {
                maxMag[fingerId, knuckleId].x = potentialMax.xMag;
            }

            if (maxMag[fingerId, knuckleId].y < potentialMax.yMag)
            {
                maxMag[fingerId, knuckleId].y = potentialMax.yMag;
            }

            if (maxMag[fingerId, knuckleId].z < potentialMax.zMag)
            {
                maxMag[fingerId, knuckleId].z = potentialMax.zMag;
            }
        }

        private void setMinMag(int fingerId, int knuckleId, MotionData potentialMin)
        {
            if (minMag[fingerId, knuckleId].x > potentialMin.xMag)
            {
                minMag[fingerId, knuckleId].x = potentialMin.xMag;
            }

            if (minMag[fingerId, knuckleId].y > potentialMin.yMag)
            {
                minMag[fingerId, knuckleId].y = potentialMin.yMag;
            }

            if (minMag[fingerId, knuckleId].z > potentialMin.zMag)
            {
                minMag[fingerId, knuckleId].z = potentialMin.zMag;
            }
        }

        public Vector3 GetAccelOffset(int fingerId, int knuckleId)
        {
            return accelOffset[fingerId, knuckleId];
        }

        public Vector3 GetGyroOffset(int fingerId, int knuckleId)
        {
            return gyroOffset[fingerId, knuckleId];
        }

        public Vector3 GetMagOffset(int fingerId, int knuckleId)
        {
            return magOffset[fingerId, knuckleId];
        }

        private void ConvertOffsetUnits()
        {
            for (int i = 0; i < TOTAL_FINGER_REFS; i++)
            {
                if (i == (int)Finger.HAND)
                {
                    /* Set hand acceleration offsets */
                    accelOffset[i, 1].x = accelOffset[i, 1].x * ACCEL_SENSITIVITY * MULTIPLIER;
                    accelOffset[i, 1].y = accelOffset[i, 1].y * ACCEL_SENSITIVITY * MULTIPLIER;
                    accelOffset[i, 1].z = accelOffset[i, 1].z * ACCEL_SENSITIVITY * MULTIPLIER;
                    /* Set hand gyro offsets */
                    gyroOffset[i, 1].x = gyroOffset[i, 1].x * GYRO_SENSITIVITY * MULTIPLIER;
                    gyroOffset[i, 1].y = gyroOffset[i, 1].y * GYRO_SENSITIVITY * MULTIPLIER;
                    gyroOffset[i, 1].z = gyroOffset[i, 1].z * GYRO_SENSITIVITY * MULTIPLIER;
                    /* Set hand mag offsets */
                    magOffset[i, 1].x = magOffset[i, 1].x * MAG_SENSITIVITY;
                    magOffset[i, 1].y = magOffset[i, 1].y * MAG_SENSITIVITY;
                    magOffset[i, 1].z = magOffset[i, 1].z * MAG_SENSITIVITY;
                }
                else
                {
                    for (int j = 0; j < TOTAL_KNUCKLE_REFS; j++)
                    {
                        /* Set hand acceleration offsets */
                        accelOffset[i, j].x = accelOffset[i, j].x * ACCEL_SENSITIVITY * MULTIPLIER;
                        accelOffset[i, j].y = accelOffset[i, j].y * ACCEL_SENSITIVITY * MULTIPLIER;
                        accelOffset[i, j].z = accelOffset[i, j].z * ACCEL_SENSITIVITY * MULTIPLIER;
                        /* Set hand gyro offsets */
                        gyroOffset[i, j].x = gyroOffset[i, j].x * GYRO_SENSITIVITY * MULTIPLIER;
                        gyroOffset[i, j].y = gyroOffset[i, j].y * GYRO_SENSITIVITY * MULTIPLIER;
                        gyroOffset[i, j].z = gyroOffset[i, j].z * GYRO_SENSITIVITY * MULTIPLIER;
                        /* Set hand mag offsets */
                        magOffset[i, j].x = magOffset[i, j].x * MAG_SENSITIVITY;
                        magOffset[i, j].y = magOffset[i, j].y * MAG_SENSITIVITY;
                        magOffset[i, j].z = magOffset[i, j].z * MAG_SENSITIVITY;
                    }
                }
            }
        }
    }
}
