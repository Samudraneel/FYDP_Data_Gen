import math
import matplotlib.pyplot as plt
import matplotlib.animation as animation
# from matplotlib import style
import serial
import struct
import time
import madgwick_py.madgwickahrs as Madgwick
# import madgwick
import madgwick_py.quaternion as Q
import numpy as np
from scipy.signal import butter, lfilter, freqz

# matplotlib style
# style.use('fivethirtyeight')

# sensitivities
SENSITIVITY_ACCELEROMETER_2  = 0.000061
SENSITIVITY_ACCELEROMETER_4  = 0.000122
SENSITIVITY_ACCELEROMETER_8  = 0.000244
SENSITIVITY_ACCELEROMETER_16 = 0.000732
SENSITIVITY_GYROSCOPE_245    = 0.00875
SENSITIVITY_GYROSCOPE_500    = 0.0175
SENSITIVITY_GYROSCOPE_2000   = 0.07
SENSITIVITY_MAGNETOMETER_4   = 0.00014
SENSITIVITY_MAGNETOMETER_8   = 0.00029
SENSITIVITY_MAGNETOMETER_12  = 0.00043
SENSITIVITY_MAGNETOMETER_16  = 0.00058
sens = (SENSITIVITY_ACCELEROMETER_2, SENSITIVITY_ACCELEROMETER_2, SENSITIVITY_ACCELEROMETER_2,
        SENSITIVITY_GYROSCOPE_245, SENSITIVITY_GYROSCOPE_245, SENSITIVITY_GYROSCOPE_245,
        SENSITIVITY_MAGNETOMETER_4, SENSITIVITY_MAGNETOMETER_4, SENSITIVITY_MAGNETOMETER_4)


def to_rad(x):
    return (3.14159265 / 180) * x

def to_deg(x, prev):
    # if x < 0:
    #     return 180/math.pi * x + 360

    # if 360*0.92 < abs(x - prev) < 360*1.08:
    #     return 180/math.pi * (x % 360)

    if 2*math.pi*0.9 < x - prev < 2*math.pi*1.1:
        return 180/math.pi * (x - 2*math.pi)
    elif 2*math.pi*0.9 < prev - x < 2*math.pi*1.1:
        return 180/math.pi * (x + 2*math.pi)
    return 180/math.pi * x


class Driver():

    def __init__(self):
        self.ser = serial.Serial('/dev/cu.usbserial-DN05ACYN', 1152000, timeout=3)
        self.xAccOffsets = [[0,0,0] for i in range(6)]
        self.yAccOffsets = [[0,0,0] for i in range(6)]
        self.zAccOffsets = [[0,0,0] for i in range(6)]
        self.xGyroOffsets = [[0,0,0] for i in range(6)]
        self.yGyroOffsets = [[0,0,0] for i in range(6)]
        self.zGyroOffsets = [[0,0,0] for i in range(6)]
        self.xMagOffsets = [[0,0,0] for i in range(6)]
        self.yMagOffsets = [[0,0,0] for i in range(6)]
        self.zMagOffsets = [[0,0,0] for i in range(6)]

    def continuousRead(self, duration):

        self.ser.write("data")
        data = [ [ [],[],[] ] for i in range(6)]
        start = time.time()
        counts = [[0,0,0] for i in range(6)]
        while (time.time() - start < duration):
            fmt = '<' + 'hBBhhhhhhhhh'
            newData = self.ser.read(352)        # = length(fmt) * 16

            bytes_remaining = 352
            offset = 0
            while (bytes_remaining > 0):
                (delim, finger, knuckle, xAcc, yAcc, zAcc, xGyro, yGyro, zGyro, xMag, yMag, zMag) = struct.unpack(fmt, newData[offset:offset+22])
                bytes_remaining -= 22;
                offset += 22

                # multiply each value that was read by its scaling factor
                # raw = (tuple(a * b for a,b in zip((xAcc, yAcc, zAcc, xGyro, yGyro, zGyro, xMag, yMag, zMag), sens)))

                print(finger, knuckle)

                # validate finger and knuckle
                if finger < 4 and knuckle > 2:
                    print("Invalid finger and/or knuckle %d %d"%(finger, knuckle))
                    continue
                elif finger == 5 and knuckle != 1:
                    print("Invalid finger and/or knuckle %d %d"%(finger, knuckle))
                    continue
                elif finger > 5:
                    print("Invalid finger and/or knuckle %d %d"%(finger, knuckle))
                    continue

                # subtract each value by its offset
                counts[finger][knuckle] += 1
                data[finger][knuckle].append([
                    xAcc * sens[0] - self.xAccOffsets[finger][knuckle],
                    yAcc * sens[1] - self.yAccOffsets[finger][knuckle],
                    zAcc * sens[2] - self.zAccOffsets[finger][knuckle],
                    xGyro * sens[3] - self.xGyroOffsets[finger][knuckle],
                    yGyro * sens[4] - self.yGyroOffsets[finger][knuckle],
                    zGyro * sens[5] - self.zGyroOffsets[finger][knuckle],
                    xMag * sens[6] - self.xMagOffsets[finger][knuckle],
                    yMag * sens[7] - self.yMagOffsets[finger][knuckle],
                    zMag * sens[8] - self.zMagOffsets[finger][knuckle]
                ])

        self.ser.write("stop")
        output = self.ser.read(10000)
        print(output)
        time.sleep(0.5)
        print("Done reading")
        for finger in data:
            print(len(finger[0]), len(finger[1]), len(finger[2]))
        print(counts)
        return data

    def calibrate(self):
        print("Starting calibration")

        print("Performing 5-second read for gyro and accel calibration.")
        data = self.continuousRead(5)

        finger_idx = 0
        for finger in data:
            knuckle_idx = 0
            for knuckle in finger:
                if len(knuckle) > 0:
                    allxAcc = [d[0] for d in knuckle]
                    allYAcc = [d[1] for d in knuckle]
                    allZacc = [d[2] for d in knuckle]
                    allxGyro = [d[3] for d in knuckle]
                    allyGyro = [d[4] for d in knuckle]
                    allzGyro = [d[5] for d in knuckle]

                    self.xAccOffsets[finger_idx][knuckle_idx] = sum(allxAcc)/len(allxAcc)
                    self.yAccOffsets[finger_idx][knuckle_idx] = sum(allYAcc)/len(allYAcc)
                    self.zAccOffsets[finger_idx][knuckle_idx] = sum(allZacc)/len(allZacc) - 1
                    self.xGyroOffsets[finger_idx][knuckle_idx] = sum(allxGyro)/len(allxGyro)
                    self.yGyroOffsets[finger_idx][knuckle_idx] = sum(allyGyro)/len(allyGyro)
                    self.zGyroOffsets[finger_idx][knuckle_idx] = sum(allzGyro)/len(allzGyro)

                knuckle_idx += 1
            finger_idx += 1

        # print("Preparing for magnetometer calibration. Start moving IMU slowly in figure 8's to cover a sphere.")
        # time.sleep(2)
        # print("Performing magnetometer calibration")
        # data = self.continuousRead(15)

        # finger_idx = 0
        # for finger in data:
        #     knuckle_idx = 0
        #     for knuckle in finger:
        #         if len(knuckle) > 0:
        #             allXMag = [d[6] for d in knuckle]
        #             allYMag = [d[7] for d in knuckle]
        #             allZMag = [d[8] for d in knuckle]

        #             self.xMagOffsets[finger_idx][knuckle_idx] = (max(allXMag) + min(allXMag))/2
        #             self.yMagOffsets[finger_idx][knuckle_idx] = (max(allYMag) + min(allYMag))/2
        #             self.zMagOffsets[finger_idx][knuckle_idx] = (max(allZMag) + min(allZMag))/2

        #         knuckle_idx += 1
        #     finger_idx += 1

        # offsets = (self.xAccOffsets, self.yAccOffsets, self.zAccOffsets,
        #     self.xGyroOffsets, self.yGyroOffsets, self.zGyroOffsets,
        #     self.xMagOffsets, self.yMagOffsets, self.zMagOffsets)
        # print("offsets:")
        # print(offsets)

        print("Done calibrating. Put the device back down.")
        time.sleep(2)


def dump_data(data):
    num_samples = len(data[0][0])
    with open("data.txt", "w") as f:
        for i in range(num_samples):
            finger_idx = 0
            for finger in data:
                knuckle_idx = 0
                for knuckle in finger:
                    # print(finger_idx, knuckle_idx, len(knuckle))
                    if finger_idx == 5 and (knuckle_idx == 0 or knuckle_idx == 2):
                        knuckle_idx += 1
                        continue
                    f.write("%d %d %d %d %d %d %d %d %d %d %d\r\n"%(
                        finger_idx,
                        knuckle_idx,
                        knuckle[i][0]/sens[0],
                        knuckle[i][1]/sens[1],
                        knuckle[i][2]/sens[2],
                        knuckle[i][3]/sens[3],
                        knuckle[i][4]/sens[4],
                        knuckle[i][5]/sens[5],
                        knuckle[i][6]/sens[6],
                        knuckle[i][7]/sens[7],
                        knuckle[i][8]/sens[8]
                    ))
                    knuckle_idx += 1
                finger_idx += 1

def lpf(val, prev):
    a = 0.1
    return a*val + (1-a)*prev

if __name__ == "__main__":

    # init driver, calibrate, and read for 5 seconds
    driver = Driver()
    driver.calibrate()
    all_data = driver.continuousRead(10)
    dump_data(all_data)

    data = all_data[4][2]
    allxAcc = [d[0] for d in data]
    allYAcc = [d[1] for d in data]
    allZacc = [d[2] for d in data]

    allxGyro = [d[3] for d in data]
    allyGyro = [d[4] for d in data]
    allzGyro = [d[5] for d in data]

    allXMag = [d[6] for d in data]
    allYMag = [d[7] for d in data]
    allZMag = [d[8] for d in data]

    plt.figure(1)
    plt.title("Accelerations (g)")
    plt.plot([i for i in range(len(allxAcc))], allxAcc, '-o')
    plt.plot([i for i in range(len(allYAcc))], allYAcc, '-x')
    plt.plot([i for i in range(len(allZacc))], allZacc, '-')

    plt.figure(2)
    plt.title("Gyro (dps)")
    plt.plot([i for i in range(len(allxGyro))], allxGyro, '-o')
    plt.plot([i for i in range(len(allyGyro))], allyGyro, '-x')
    plt.plot([i for i in range(len(allzGyro))], allzGyro, '-')

    plt.figure(3)
    plt.title("Mag")
    plt.plot([i for i in range(len(allXMag))], allXMag, '-o')
    plt.plot([i for i in range(len(allYMag))], allYMag, '-x')
    plt.plot([i for i in range(len(allZMag))], allZMag, '-')

    # get pitch roll and yaw of filtered data with downloaded madgwick filter
    madgwick = Madgwick.MadgwickAHRS(sampleperiod=0.013, quaternion=Q.Quaternion(1, 0, 0, 0), beta=1)
    pitch = []
    roll = []
    yaw = []
    for i in range(0, len(data)):
        # for j in range(10):
        # madgwick.update(
        #     [to_rad(allxGyro[i]), to_rad(allyGyro[i]), to_rad(allzGyro[i])],
        #     [allxAcc[i], allYAcc[i], allZacc[i]],
        #     data[6:9])

        # madgwick.update_imu(
        #     [to_rad(allxGyro[i]), to_rad(allyGyro[i]), to_rad(allzGyro[i])],
        #     [allxAcc[i], allYAcc[i], allZacc[i]])

        # (r, p, y) = madgwick.quaternion.to_euler123()
        # (r, p, y) = madgwick.quaternion.to_euler_angles()
        # xAcc = sum(allxAcc[i:i+10])/10
        # yAcc = sum(allYAcc[i:i+10])/10
        # zAcc = sum(allZacc[i:i+10])/10
        # xMag = sum(allXMag[i:i+10])/10
        # yMag = sum(allYMag[i:i+10])/10
        # zMag = sum(allZMag[i:i+10])/10

        xAcc = allxAcc[i]
        yAcc = allYAcc[i]
        zAcc = allZacc[i]
        xMag = allXMag[i]
        yMag = allYMag[i]
        zMag = allZMag[i]

        p = np.arctan2(-xAcc, (np.sqrt ((yAcc * yAcc) + (zAcc * zAcc))))
        r = np.arctan2(yAcc, (np.sqrt((xAcc * xAcc) + (zAcc * zAcc))))
        # y = np.arctan2(zAcc, np.sqrt(xAcc*xAcc + zAcc*zAcc))

        xh = xMag * np.cos(p) + zMag * np.sin(p);
        yh = xMag * np.sin(r) * np.sin(p) + yMag * np.cos(r) - zMag * np.sin(r) * np.cos(p);
        # zh = -xMag * np.cos(r) * np.sin(p) + yMag * np.sin(r) + yMag * np.cos(r) * np.cos(p);
        y = np.arctan2(yh, xh)

        if i == 0:
            pitch.append(to_deg(p, 0))
            roll.append(to_deg(r, 0))
            yaw.append(to_deg(y, 0))
        else:
            pitch.append(to_deg(p, math.pi/180 * pitch[-1]))
            roll.append(to_deg(r, math.pi/180 * roll[-1]))
            yaw.append(to_deg(y, math.pi/180 * yaw[-1]))

            pitch[-1] = lpf(pitch[-1], pitch[-2])
            roll[-1] = lpf(roll[-1], roll[-2])
            yaw[-1] = lpf(yaw[-1], yaw[-2])


    # # get pitch roll and yaw of filtered data with madgwick filter converted from C
    # pitch = []
    # roll = []
    # yaw = []
    # for i in range(len(data)):
    #     for j in range(10):
    #         madgwick.MadgwickQuaternionUpdate(
    #             filteredXAcc[i], filteredYAcc[i], filteredZAcc[i],
    #             to_rad(filteredXGyro[i]), to_rad(filteredYGyro[i]), to_rad(filteredZGyro[i]),
    #             data[i][6], data[i][7], data[i][8],
    #             0.004
    #         )
    #     quat = Q.Quaternion(*(madgwick.q))
    #     (r, p, y) = quat.to_euler_angles()
    #     pitch.append(180/math.pi * p)
    #     roll.append(180/math.pi * r)
    #     yaw.append(180/math.pi * y)

    # # get pitch roll and yaw from acceleration and magnetic field
    # # taken from https://engineering.stackexchange.com/questions/3348/calculating-pitch-yaw-and-roll-from-mag-acc-and-gyro-data
    # pitch = []
    # roll = []
    # yaw = []

    # for i in range(len(data)):
    #     p = 180 * math.atan2(filteredXAcc[i], math.sqrt(filteredYAcc[i]*filteredYAcc[i] + filteredZAcc[i]*filteredZAcc[i]))
    #     r = 180 * math.atan2(filteredYAcc[i], math.sqrt(filteredXAcc[i]*filteredXAcc[i] + filteredZAcc[i]*filteredZAcc[i]))
    #     y = 180 * math.atan2(filteredZAcc[i], math.sqrt(filteredXAcc[i]*filteredXAcc[i] + filteredZAcc[i]*filteredZAcc[i]))

    #     pitch.append(p)
    #     roll.append(r)
    #     yaw.append(y)

    # # low pass filter the pitch roll and yaw
    # order = 6
    # fs = 25       # sample rate, Hz
    # cutoff = 1    # desired cutoff frequency of the filter, Hz

    # # Get the filter coefficients so we can check its frequency response.
    # b, a = butter_lowpass(cutoff, fs, order)

    # pitch = butter_lowpass_filter(pitch, cutoff, fs, order)
    # roll = butter_lowpass_filter(roll, cutoff, fs, order)
    # yaw = butter_lowpass_filter(yaw, cutoff, fs, order)

    # plot pitch roll and yaw
    plt.figure(4)
    plt.title("Pitch (degrees)")
    print(len(pitch))
    plt.plot([i for i in range(len(pitch))], pitch, '-o')

    plt.figure(5)
    plt.title("Roll (degrees)")
    plt.plot([i for i in range(len(roll))], roll, '-x')

    plt.figure(6)
    plt.title("Yaw (degrees)")
    plt.plot([i for i in range(len(yaw))], yaw, '-')

    plt.show()
