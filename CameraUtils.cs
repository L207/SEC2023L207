using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace RestLike
{
    static class Camera_Utils
    {
        private static int threshold = (251 * 3);
        public static bool checkTarget(float x_position , float z_position,
                                       float drone_angle, float drone_alt,
                                       ObjMap map       , byte[] CameraBytes,
                                       List<NavMap> navmap)
        {
            bool result = false;
            //start at 8 to skip image size and timestamp
            for (int k = 8; k < 512*512*3; k += 3)
            {
                //                int val = BitConverter.ToChar(CameraBytes, k)
                //                        + BitConverter.ToChar(CameraBytes, k + 1)
                //                        + BitConverter.ToChar(CameraBytes, k + 2);
                int val = CameraBytes[k]
                        + CameraBytes[k + 1]
                        + CameraBytes[k + 2];

                if (val > threshold)
                {
                    double x0 = (double)(((((double)k / 3) % 512) - 256) / 512);
                    double z0 = (double)(((((((double)k / 3) - x0)) / 512) - 256) / 512);
                    double angle = (double)drone_angle * (Math.PI / 180);

                    double x1 = +x0 * Math.Cos(angle) + z0 * Math.Sin(angle);
                    double z1 = -x0 * Math.Sin(angle) + z0 * Math.Cos(angle);

                    float x_out = ((float)x1) * drone_alt * 2 + x_position;
                    float z_out = ((float)z1) * drone_alt * 2 + z_position;

                    if (map.setTarget(x_out, z_out))
                    {
                        Console.WriteLine("TGT : " + x_out.ToString() + " " + z_out.ToString()
                                       + " : " + x1.ToString() + " " + z1.ToString());
                        navmap.Add(new NavMap(512,512,x_out,z_out));
                        result = true;
                    };
                }
            }
            return result;
        }

        public static bool checkNoVehicleInLidar(int vehicleID, 
                                           int[] vehiclesx, int[] vehiclesz,
                                           float pointx, float pointz)
        {
            bool result = true;
            int threshold = 4;
            int xoffset = 256;
            int zoffset = 256;
            int ipointx = (int)pointx + xoffset;
            int ipointz = (int)pointz + zoffset;
            for (int i = 0; i < 5; i++)
            {
                if (i != vehicleID)
                {
                    if ((vehiclesx[i] < (ipointx + threshold)) &&
                        (vehiclesx[i] > (ipointx - threshold)) &&
                        (vehiclesz[i] < (ipointz + threshold)) &&
                        (vehiclesz[i] > (ipointz - threshold)))
                    {
                        result = false;
                    }
                }
            }
            return result;
            
        }
        

    }

}
