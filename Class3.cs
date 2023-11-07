using System;
using System.Collections.Generic;
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
        public DroneNav(float[] x_waypoints, float[] z_waypoints)
        {
            x_waypoints = waypoints_x;
            z_waypoints = waypoints_z;
        }
        
        public void testWaypoint(float x_position, float z_position, float threshold)
        {
            if (Math.Sqrt((x_position - waypoints_x[waypoint]) *
                (z_position - waypoints_z[waypoint])) < threshold)
            {
                if (waypoint != waypoints_x.Length)
                    waypoint++;
            }
        }

        public float getHeading(float x_position, float z_position)
        {
            return (float)(Math.Atan2((double)waypoints_z[waypoint] - (double)z_position,
                              (double)waypoints_x[waypoint] - (double)x_position) * 
                              180 / Math.PI);
        }
    }
}
