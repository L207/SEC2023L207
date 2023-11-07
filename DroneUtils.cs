using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestLike
{
    class DroneNav
    {
        private float[] waypoints_x;
        private float[] waypoints_z;
        private int waypoint = 0;
        private float prevwaypoint_x = 0;
        private float prevwaypoint_z = -225;
        private float speedlimit = 18.5f;
        public DroneNav(float[] x_waypoints, float[] z_waypoints)
        {
            waypoints_x = x_waypoints;
            waypoints_z = z_waypoints;
        }
        
        public void testWaypoint(float x_position, float z_position, float threshold)
        {
            if ((float)Math.Sqrt(Math.Pow((double)(x_position - waypoints_x[waypoint]),2) +
                                 Math.Pow((double)(z_position - waypoints_z[waypoint]),2))
                                 < threshold)
            {
                prevwaypoint_x = waypoints_x[waypoint];
                prevwaypoint_z = waypoints_z[waypoint];
                if (waypoint < waypoints_x.Length - 1)
                {
                    waypoint++;
                } else
                {
                    waypoint = 0;
                }
                    
            }
        }

        public float getHeading(float x_position, float z_position)
        {
            testWaypoint(x_position, z_position, 5f);
            float waypointlead = 10f;
            float drone2waypoint = getMagnitude(x_position, z_position, waypoints_x[waypoint], waypoints_z[waypoint]);
            float waypointdist = getMagnitude(prevwaypoint_x, prevwaypoint_z, waypoints_x[waypoint], waypoints_z[waypoint]);
            float percentdist = 1 - ((drone2waypoint - 10) / waypointdist);
            float v_waypoint_x = prevwaypoint_x * (1 - percentdist) + waypoints_x[waypoint] * (percentdist);
            float v_waypoint_z = prevwaypoint_z * (1 - percentdist) + waypoints_z[waypoint] * (percentdist);

            return (float)(0 + Math.Atan2((double)v_waypoint_x - (double)x_position,
                                      (double)v_waypoint_z - (double)z_position) * 180 / Math.PI);
        }

        public float getSpeed(float x_position, float z_position, float vehiclespeedx, float vehiclespeedz)
        {
            float vehiclespeedm = getMagnitude(0f, 0f, vehiclespeedx, vehiclespeedz);
            return (Math.Min(1f + 
                            getMagnitude(x_position, z_position, waypoints_x[waypoint], waypoints_z[waypoint])
                           - vehiclespeedm*4, speedlimit));
            //return Math.Min(0f + 0.4f * 
            //                getMagnitude(x_position,z_position,waypoints_x[waypoint],waypoints_z[waypoint])
            //                , 20f);
        }

        private float getMagnitude(float x_position, float z_position, float waypoint_x, float waypoint_z)
        {
            return (float)Math.Sqrt(Math.Pow((double)(x_position - waypoint_x), 2) +
                                    Math.Pow((double)(z_position - waypoint_z), 2));
        }

        public string getwaypointstring()
        {
            return ("x_tgt: " + (waypoints_x[waypoint]).ToString() + " z_tgt:" + (waypoints_z[waypoint]).ToString());
        }
    }
}
