using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommsSample
{
    public enum T_map_voxel
    {
        None,
        Visited,
        Target,
        Obstacle
    }

    public class ObjMap
    {
        private int x_len;
        private int z_len;
        private T_map_voxel[,] map;
        public ObjMap(int x_length, int z_length)
        {
            x_len = x_length;
            z_len = z_length;
            map = new T_map_voxel[x_length, z_length];
            for (int i = 0; i < x_len; i++)
            {
                for (int j = 0; j < z_len; j++)
                {
                    map[i, j] = T_map_voxel.None;
                }
            }
        }

        //There is no need for a "setNone" it is part of the initialisation, and never returns to this state.
        public void setObstacle(int x_position, int z_position)
        {
            //set obstacle state can't be overridden, so just set it.
            map[x_position, z_position] = T_map_voxel.Obstacle;
        }

        public void setTarget(int x_position, int z_position)
        {
            //Visited and Obstacle can't switch to Target.
            //If already Target no reason to switch to Target.
            //only check for None applies.
            if (map[x_position, z_position] == T_map_voxel.None)
            {
                map[x_position, z_position] = T_map_voxel.Target;
            }
        }
        public void setVisited(int x_position, int z_position)
        {
            //only obstacle can't transition to Visited.
            //not an issue if Visited set to visited.
            //only need to check for obstacle.
            if (map[x_position, z_position] != T_map_voxel.Obstacle)
            {
                map[x_position, z_position] = T_map_voxel.Visited;
            }
        }

        public T_map_voxel getVoxel(int x_position, int z_position)
        {
            return map[x_position, z_position];
        }

    }

    public class NavMap
    {
        private int x_len;
        private int z_len;
        private int[,] navmap;
        private int badvalue;


        public NavMap(int x_length, int z_length)
        {
            //the initialisation is needed for any review of the map
            //so call instead of do here.
            MapInit(x_length, z_length);
        }

        //needed for both initialisation and re-evaluation of map
        public void MapInit(int x_length, int z_length)
        {
            x_len = x_length;
            z_len = z_length;
            badvalue = int.MaxValue; //value should never be able to be larger than this
            navmap = new int[x_length, z_length];
            for (int i = 0; i < x_len; i++)
            {
                for (int j = 0; j < z_len; j++)
                {
                    navmap[i, j] = badvalue;
                }
            }
        }

        private void SetValue(ObjMap map, int x_loc, int z_loc, int value)
        {
            //check location exists
            if ((x_loc > -1) && (z_loc > -1) && (x_loc < x_len) && (z_loc < z_len))
            {
                //only update this location if is OK to use
                if ((navmap[x_loc,z_loc] > value) && (map.getVoxel(x_loc, z_loc) != T_map_voxel.Obstacle))
                {
                    navmap[x_loc, z_loc] = value;
                }
            }
        }

        public void EvalRoute(ObjMap map, int tgt_x, int tgt_z, int truck_x, int truck_z)
        {
            MapInit(x_len, z_len);
            bool EvalContinue = true;
            int loop = 0;
            while (EvalContinue)
            {
                for (int i = 0; i < x_len; i++)
                {
                    for (int j = 0; j < z_len; j++)
                    {
                        if (EvalContinue && (navmap[i, j] == loop))
                        {
                            SetValue(map, i + 1, j, loop + 1);
                            SetValue(map, i - 1, j, loop + 1);
                            SetValue(map, i, j + 1, loop + 1);
                            SetValue(map, i, j - 1, loop + 1);
                            //If truck has been found no reason to keep evaluating
                            if ((i == truck_x) && (j == truck_z))
                            {
                                EvalContinue = false;
                            }
                        }
                    }
                }
            }
        }

    }
}
