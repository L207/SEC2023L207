using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RestLike
{
    public enum T_map_voxel
    {
        None,
        Seen,
        Visited,
        Target,
        Obstacle,
        nearwall1,
        Visitednearwall1
    }

    public class ObjMap
    {
        private int x_len;
        private int z_len;
        private int x_offset;
        private int z_offset;
        private T_map_voxel[,] map;
        private int setseenradius = 2;
        //private NavMap[,] navmaps;
        public ObjMap(int x_length, int z_length)
        {
            x_len = x_length;
            z_len = z_length;
            x_offset = x_length / 2;
            z_offset = z_length / 2;
            map = new T_map_voxel[x_length, z_length];
            //navmaps = new NavMap[x_length, z_length];
            for (int i = 0; i < x_len; i++)
            {
                for (int j = 0; j < z_len; j++)
                {
                    map[i, j] = T_map_voxel.None;
                    //navmaps[i, j] = new NavMap(x_length, z_length);
                }
            }
        }

        private bool checkpos(int x, int z)
        {
            return ((x > -1) && (x < x_len) && (z > -1) && (z < z_len));
        }

        private bool checkSurround(int x, int z, T_map_voxel type, int range)
        {
            bool result = true;
            for (int i = -range; i < (range + 1); i++)
            {
                for (int j = -range; j < (range + 1); j++)
                {
                    result &= checkpos(x + i, z + j) && getVoxel(x + i, z + j) != type;
                }
            }
            return result;
        }

        private void setNearWall(int x, int z)
        {
            int threshold = 1;
            for (int i = -threshold; i < threshold+1; i++)
            {
                for (int j = -threshold; j < threshold+1; j++)
                {
                    if (checkpos(x + i, z + j) && Math.Abs(i) + Math.Abs(j) < threshold + 1)
                    {
                        if (getVoxel(x + i, z + j) == T_map_voxel.None || getVoxel(x + i, z + j) == T_map_voxel.Seen)
                            map[x + i, z + j] = T_map_voxel.nearwall1;
                        if (getVoxel(x + i, z + j) == T_map_voxel.Visited)
                            map[x + i, z + j] = T_map_voxel.Visitednearwall1;
                    }
                }
            }
        }

        //There is no need for a "setNone" it is part of the initialisation, and never returns to this state.
        
        public void setSeen(float x_position, float z_position)
        {
            setSeen((int)x_position + x_offset, (int)z_position + z_offset);
        }

        public void setSeen(int x_position, int z_position)
        {
            if (checkpos(x_position, z_position) && getVoxel(x_position, z_position) == T_map_voxel.None)
            {
                map[x_position, z_position] = T_map_voxel.Seen;
            }

            for (int i = -setseenradius; i < setseenradius + 1; i++)
            {
                for (int j = -setseenradius; j < setseenradius + 1; j++)
                {
                    if (checkpos(x_position + i, z_position + j) && map[x_position + i, z_position + j] == T_map_voxel.None)
                        map[x_position + i, z_position + j] = T_map_voxel.Seen;
                }
            }
        }
        
        public bool setObstacle(float x_position, float z_position)  //returns true if it was a target before, now deleted.
        {
            return setObstacle((int)x_position + x_offset, (int)z_position + z_offset);
        }
        public bool setObstacle(int x_position, int z_position) //returns true if it was a target before, now deleted.
        {
            bool result = false;
            ////set obstacle state can't be overridden, so just set it.
            //ostacle can't override visited, to stop getting stuck going through small gaps
            //sometimes hitting a wall hard enough can set an obstacle thats real to visit - need both!
            if (checkpos(x_position, z_position))// && map[x_position, z_position] != T_map_voxel.Visited)
            {
                result = map[x_position, z_position] == T_map_voxel.Target;
                map[x_position, z_position] = T_map_voxel.Obstacle;
                setNearWall(x_position, z_position);
            }
            return result;
        }

        public bool setTarget(float x_position, float z_position)
        {
            return setTarget((int)x_position + x_offset, (int)z_position + z_offset);
        }
        public bool setTarget(int x_position, int z_position)
        {
            bool result = false;
            //Visited and Obstacle can't switch to Target.
            //If already Target no reason to switch to Target.
            //only check for None applies.
            if (checkpos(x_position,z_position) && (
                                                    map[x_position, z_position] == T_map_voxel.None ||
                                                    map[x_position, z_position] == T_map_voxel.Seen ||
                                                    map[x_position, z_position] == T_map_voxel.nearwall1) )
            {
                if (checkSurround(x_position, z_position, T_map_voxel.Target, 3))
                {
                    map[x_position, z_position] = T_map_voxel.Target;
                    result = true;
                }
            }
            return result;
        }
        
        public bool setVisited(float x_position, float z_position)  //returns true if it was a target before, now deleted.
        {
            return setVisited((int)x_position + x_offset, (int)z_position + z_offset);
        }
        public bool setVisited(int x_position, int z_position)  //returns true if it was a target before, now deleted.
        {
            //only obstacle can't transition to Visited.
            // - NO LONGER TRUE, IF YOU'VE BEEN THERE, YOU CAN DRIVE THERE IS NEW RULE.
            //not an issue if Visited set to visited.
            //only need to check for obstacle.
            bool result = false;
            //overridden any check as sometimes a bug can occur where obstacle is written over the tank.
            if (checkpos(x_position, z_position))// && map[x_position, z_position] != T_map_voxel.Obstacle)
            {
                if (map[x_position, z_position] == T_map_voxel.Target)
                    result = true;
                if (map[x_position, z_position] == T_map_voxel.nearwall1)
                {
                    map[x_position, z_position] = T_map_voxel.Visitednearwall1;
                }
                else
                {
                    map[x_position, z_position] = T_map_voxel.Visited;
                }
            }
            return result;
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
        private int [][,] navmap = new int[2][,];
        private int badvalue;
        private int tgt_pos_x;
        private int tgt_pos_z;
        private int x_offset;
        private int z_offset;
        private int [] upperloop = new int[2];
        private int writesel = 1; //write to this, read from other;
        private bool writelock = false;
        private bool evallock = false;
        private int seenoffset = 1;
        private int nearwall1offset = 25;
        private int notseenoffset = 2;
        //Thread navthread = new Thread;

        private bool checkpos(int x, int z)
        {
            return ((x > -1) && (x < x_len) && (z > -1) && (z < z_len));
        }


        public int getbadvalue()
        {
            return badvalue;
        }
        public NavMap(int x_length, int z_length)
        {
            //the initialisation is needed for any review of the map
            //so call instead of do here.
            MapInit(x_length, z_length);
        }

        public NavMap(int x_length, int z_length, float tgt_x, float tgt_z)
        {
            x_offset = x_length / 2;
            z_offset = z_length / 2;
            ObjInit(x_length, z_length, (int)tgt_x + x_offset, (int)tgt_z + z_offset);
        }
        public NavMap(int x_length, int z_length, int tgt_x, int tgt_z)
        {
            x_offset = x_length / 2;
            z_offset = z_length / 2;
            ObjInit(x_length, z_length, tgt_x, tgt_z);
        }
        private void ObjInit(int x_length, int z_length, int tgt_x, int tgt_z)
        {
            MapInit(x_length, z_length);
            writesel = 0;
            MapInit(x_length, z_length);
            upperloop[0] = badvalue;
            upperloop[1] = badvalue;
            tgt_pos_x = tgt_x;
            tgt_pos_z = tgt_z;
        }

        //needed for both initialisation and re-evaluation of map
        public void MapInit(int x_length, int z_length)
        {
            x_len = x_length;
            z_len = z_length;
            //badvalue = int.MaxValue; //value should never be able to be larger than this
            badvalue = x_len * z_len;
            navmap[writesel] = new int[x_length, z_length];
            for (int i = 0; i < x_len; i++)
            {
                for (int j = 0; j < z_len; j++)
                {
                    navmap[writesel][i, j] = badvalue;
                }
            }
        }

        private bool SetValue(ObjMap map, int x_loc, int z_loc, int value, bool dontoverride)
        {
            bool result = false;
            //check location exists
            if ((x_loc > -1) && (z_loc > -1) && (x_loc < x_len) && (z_loc < z_len))
            {
                int localvalue = navmap[writesel][x_loc, z_loc];
                if (dontoverride)
                {
                    switch (map.getVoxel(x_loc, z_loc))
                    {
                        case T_map_voxel.None:
                            if (value + notseenoffset < localvalue)
                            {
                                navmap[writesel][x_loc, z_loc] = value + notseenoffset;
                                result = true;
                            }
                            break;
                        case T_map_voxel.Seen:
                            if (value + seenoffset < localvalue)
                            {
                                navmap[writesel][x_loc, z_loc] = value + seenoffset;
                                result = true;
                            }
                            break;
                        case T_map_voxel.Visited:
                            if (value + seenoffset < localvalue)
                            {
                                navmap[writesel][x_loc, z_loc] = value + seenoffset;
                                result = true;
                            }
                            break;
                        case T_map_voxel.Target:
                            if (value + seenoffset < localvalue)
                            {
                                navmap[writesel][x_loc, z_loc] = value + seenoffset;
                                result = true;
                            }
                            break;
                        case T_map_voxel.nearwall1:
                            if (value + nearwall1offset < localvalue)
                            {
                                navmap[writesel][x_loc, z_loc] = value + nearwall1offset;
                                result = true;
                            }
                            break;
                        case T_map_voxel.Visitednearwall1:
                            if (value + notseenoffset < localvalue)
                            {
                                navmap[writesel][x_loc, z_loc] = value + nearwall1offset;
                                result = true;
                            }
                            break;
                        default:
                            break;
                    }
                } else
                {
                    navmap[writesel][x_loc, z_loc] = value;
                }
            }
            return result;
        }

        public void setTargetPos(float tgt_x, float tgt_z) //used for virtual targets only
        {
            tgt_pos_x = (int)tgt_x + x_offset;
            tgt_pos_z = (int)tgt_z + z_offset;
        }

        public void getTargetPos(out int tgt_x, out int tgt_z)
        {
            tgt_x = tgt_pos_x;
            tgt_z = tgt_pos_z;
        }

        public bool EvalRoute(ObjMap map, float truck_x, float truck_z) //returns true if on target or target bad;
        {
            return EvalRoute(map, (int)truck_x + x_offset, (int)truck_z + z_offset);
        }

        public bool EvalRoute(ObjMap map, int truck_x, int truck_z)
        {
            return EvalRoute(map, truck_x, truck_z, false);
        }
        public bool EvalRoute(ObjMap map, int truck_x, int truck_z, bool no_tgt_mode)
        {
            if (evallock)
                return false;
            evallock = true;
            Queue<int>[] xposQ = new Queue<int>[2];
            xposQ[0] = new Queue<int>();
            xposQ[1] = new Queue<int>();
            Queue<int>[] zposQ = new Queue<int>[2];
            zposQ[0] = new Queue<int>();
            zposQ[1] = new Queue<int>();
            int Qsel = 0; //swaps active queue object
            bool result = false;
            MapInit(x_len, z_len);
            bool EvalContinue = true;
            int localvalue = 0;
            SetValue(map, tgt_pos_x, tgt_pos_z, localvalue, false);
            int xpos = Math.Min(Math.Max(0, tgt_pos_x), x_len - 1); //correction for virtual target
            int zpos = Math.Min(Math.Max(0, tgt_pos_z), z_len - 1); //correction for virtual target
            xposQ[Qsel].Enqueue(xpos);
            zposQ[Qsel].Enqueue(zpos);
            if ((!no_tgt_mode) && ((tgt_pos_x == truck_x && tgt_pos_z == truck_z) || (map.getVoxel(tgt_pos_x, tgt_pos_z) != T_map_voxel.Target)))
                return true;
            while(EvalContinue)
            {
                EvalContinue = (xposQ[Qsel].Count > 0);
                while (xposQ[Qsel].Count > 0)
                {
                    //EvalContinue = false;
                    xpos = xposQ[Qsel].Dequeue();
                    zpos = zposQ[Qsel].Dequeue();
                    localvalue = navmap[writesel][xpos,zpos];
                    if (SetValue(map, xpos + 1, zpos, localvalue, true))
                    {
                        xposQ[1 - Qsel].Enqueue(xpos + 1);
                        zposQ[1 - Qsel].Enqueue(zpos);
                        //EvalContinue = true;
                    }
                    if (SetValue(map, xpos - 1, zpos, localvalue, true))
                    {
                        xposQ[1 - Qsel].Enqueue(xpos - 1);
                        zposQ[1 - Qsel].Enqueue(zpos);
                        //EvalContinue = true;
                    }
                    if (SetValue(map, xpos, zpos + 1, localvalue, true))
                    {
                        xposQ[1 - Qsel].Enqueue(xpos);
                        zposQ[1 - Qsel].Enqueue(zpos + 1);
                        //EvalContinue = true;
                    }
                    if (SetValue(map, xpos, zpos - 1, localvalue, true))
                    {
                        xposQ[1 - Qsel].Enqueue(xpos);
                        zposQ[1 - Qsel].Enqueue(zpos - 1);
                        //EvalContinue = true;
                    }
                }
                Qsel = 1 - Qsel;

            }
            upperloop[writesel] = localvalue;
            while (writelock)
            {
                //spinlock
            }
            writesel = (writesel + 1) % 2;
            evallock = false;
            return false;
        }

        public bool EvalRouteOld(ObjMap map, int truck_x, int truck_z, bool no_tgt_mode) //returns true if on target or target bad;
        {
            if (evallock)
                return false;
            evallock = true;
            bool result = false;
            MapInit(x_len, z_len);
            bool EvalContinue = true;
            int updated = nearwall1offset + 1;
            int loop = 0;
            int curval = 0;
            SetValue(map, tgt_pos_x, tgt_pos_z, loop, false);
            if ((!no_tgt_mode) && ((tgt_pos_x == truck_x && tgt_pos_z == truck_z) || (map.getVoxel(tgt_pos_x,tgt_pos_z) != T_map_voxel.Target)))
                return true;
            while (updated > 0 && EvalContinue)
            {
                updated--;
                for (int i = 0; i < x_len; i++)
                {
                    for (int j = 0; j < z_len; j++)
                    {
                        if (EvalContinue && (navmap[writesel][i, j] == loop) && (navmap[writesel][i, j] < badvalue))
                        {
                            curval = navmap[writesel][i, j];
                            SetValue(map, i + 1, j, curval+1, true);
                            SetValue(map, i - 1, j, curval+1, true);
                            SetValue(map, i, j + 1, curval+1, true);
                            SetValue(map, i, j - 1, curval+1, true);
                            //If truck has been found no reason to keep evaluating
                            updated = nearwall1offset + 1;
                            //turn off the continue check as need to apply to all vehicles.
                            /*if ((i == truck_x) && (j == truck_z))
                            {
                                EvalContinue = false;
                            }*/
                        }
                        if (!EvalContinue)
                            break;
                    }
                }


                loop++;
            }
            upperloop[writesel] = loop;
            while (writelock)
            {
                //spinlock
            }
            writesel = (writesel + 1) % 2;
            evallock = false;
            return false;
        }

        public float getDirection(float x_position, float z_position)
        {
            return getDirection((int)x_position + x_offset, (int)z_position + z_offset);
        }

        private bool checkNoObstacleSurround(int x_position, int z_position)
        {
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if (checkpos(x_position + i, z_position + j) && navmap[1 - writesel][x_position + i, z_position + j] == badvalue)
                        return false;
                }
            }
            return true;

        }

        public float getDirection(int x_position, int z_position)
        {
            writelock = true;
            //int[,] localarea = new int[3, 3];
            //localarea[1, 1] = 0;
            float result = 0f;
            int minval = badvalue;
            int xaxis = 0;
            int zaxis = 0;

            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if (checkpos(x_position + i, z_position + j) && navmap[1 - writesel][x_position + i, z_position + j] < minval)
                    {
                        minval = navmap[1 - writesel][x_position + i, z_position + j];
                        xaxis = i;
                        zaxis = j;
                    }
                }
            }
            //partial angle method (~30 degrees)
            if (checkNoObstacleSurround(x_position, z_position))
            {
                for (int i = -2; i < 3; i++)
                {
                    for (int j = -2; j < 3; j++)
                    {
                        if (checkpos(x_position + i, z_position + j) && navmap[1 - writesel][x_position + i, z_position + j] < minval)
                        {
                            minval = navmap[1 - writesel][x_position + i, z_position + j];
                            xaxis = i;
                            zaxis = j;
                        }

                    }
                }
                //for (int j = -2; j < 5; j += 2)
                //{
                //    for (int i = -1; i < 2; i++)
                //    {
                //        if (checkpos(x_position + i, z_position + j) && navmap[1 - writesel][x_position + i, z_position + j] < minval)
                //        {
                //            minval = navmap[1 - writesel][x_position + i, z_position + j];
                //            xaxis = i;
                //            zaxis = j;
                //        }
                //    }
                //}
            }


            float xdir = 720f;

            xdir = (float)(Math.Atan2((double)xaxis, (double)zaxis) * 180f / Math.PI);

            //need to add a check to avoid crashing into corner walls
            //result = xdir + 180f;
            result = xdir;
            writelock = false;
            return result;

        }


        public float getDirectionold(int x_position, int z_position)
        {
            writelock = true;
            int myval_min1 =  getValue(x_position, z_position) + 1;
            int myval_min2 = getValue(x_position, z_position) + 2;
            float result = 0f;
            int xaxis =
                getValue(x_position + 1, z_position, myval_min1) -
                //getValue(x_position + 2, z_position, myval_min2) -
                getValue(x_position - 1, z_position, myval_min1);
                //getValue(x_position - 2, z_position, myval_min2);
            int zaxis =
                getValue(x_position, z_position + 1, myval_min1) -
                //getValue(x_position, z_position + 2, myval_min2) -
                getValue(x_position, z_position - 1, myval_min1);
                //getValue(x_position, z_position - 2, myval_min2);
                
            float xdir = 720f;

            if (xaxis == 0 && zaxis > 0)
                xdir = 00;
            if (xaxis > 0 && zaxis > 0)
                xdir = 45;
            if (xaxis > 0 && zaxis == 0)
                xdir = 90;
            if (xaxis > 0 && zaxis < 0)
                xdir = 135;
            if (xaxis == 0 && zaxis < 0)
                xdir = 180;
            if (xaxis < 0 && zaxis < 0)
                xdir = 225;
            if (xaxis < 0 && zaxis == 0)
                xdir = 270;
            if (xaxis < 0 && zaxis > 0)
                xdir = 315;

            //need to add a check to avoid crashing into corner walls
            result = xdir + 180f;
            writelock = false;
            return result;
        }

        public int getValue(int x_pos, int z_pos, int min_value)
        {
            int result = getValue(x_pos, z_pos);
            //if (result == badvalue)
            if (result > min_value)
                result = min_value;
            return result;
        }
        
        public int getValue(int x_pos, int z_pos)
        {
            int value = badvalue;
            if ((x_pos > -1 && x_pos < x_len) && (z_pos > -1 && z_pos < z_len))
            {
                value = navmap[1 - writesel][x_pos, z_pos];
            }
            return value;
        }

        public void oldgetColor(ref byte red, ref byte green, ref byte blue, int x_pos, int z_pos)
        {
            int value = navmap[1 - writesel][x_pos, z_pos];
            if (value != badvalue)
            {
                red = (byte)(100 - (100 * value) / upperloop[1 - writesel]);
                green = (byte)(100 - red);
                blue = 0;
            } else
            {
                red = 0; green = 0; blue = 0;
            }
        }

        public void getColor(ref byte red, ref byte green, ref byte blue, int x_pos, int z_pos)
        {
            int value = navmap[1 - writesel][x_pos, z_pos];
            if (value < 400)
            {
                red = (byte)(200 - (value / 2));
                green = 0;
                blue = 0;
            }
            else
            {
                red = 0; green = 0; blue = 0;
            }
        }

        public void newgetColor(ref byte red, ref byte green, ref byte blue, int x_pos, int z_pos)
        {
            float[] colarray =   new float[] { 0f, 0.3f, 0.6f, 1f };
            float[] redarray =   new float[] { 255f, 0f, 0f, 255f };
            float[] greenarray = new float[] { 0f, 255f, 0f, 0f };
            float[] bluearray = new float[] { 0f, 0f, 255f, 0f };

            float value = (float)navmap[1 - writesel][x_pos, z_pos];
            if (value != badvalue)
            {
                red =   (byte)Interpolate(value / (float)upperloop[1 - writesel], colarray, redarray);
                green = (byte)Interpolate(value / (float)upperloop[1 - writesel], colarray, greenarray);
                blue =  (byte)Interpolate(value / (float)upperloop[1 - writesel], colarray, bluearray);
            }
            else
            {
                red = 0; green = 0; blue = 0;
            }
        }

        public float Interpolate(float inval, float[] inarray, float[] outarray)
        {
            if (inarray.Length != outarray.Length)
                Console.WriteLine("WARNING INTERPOLATE ERROR");
            float outval = 0f;
            float lerp = 0f;
            for (int i = 1; i < inarray.Length; i++)
            {
                if (inarray[i] >= inval)
                {
                    lerp = (inval - inarray[i - 1]) / (inarray[i] - inarray[i - 1]);
                    outval = (outarray[i - 1] * (1 - lerp)) + (outarray[i] * lerp);
                }
            }
            if (inval <= inarray[0])
                outval = outarray[0];
            if (inval >= inarray[inarray.Length -1])
                outval = outarray[outarray.Length - 1];
            return outval;
        }

        public static void checkNavmaplist(ObjMap map, List<NavMap> navmaplist)
        {
            for (int i = navmaplist.Count -1 ; i > -1; i--)
            {
                if (map.getVoxel(navmaplist.ElementAt(i).tgt_pos_x,
                                 navmaplist.ElementAt(i).tgt_pos_z) != T_map_voxel.Target)
                {
                    navmaplist.RemoveAt(i);
                }
            }
        }
    }

    /*
    public class NavDecision
    {
        int VehicleNumIndex = -1;
        int TgtNumIndex = -1;
        int BadValue;
        int ShortestMaxDist;
        NavDecision[] NavDecisions;
        int VehicleListCount;
        int TgtListCount;
        List<int> VehList;
        List<int> TgtList;
        int[] vehx = new int[5];
        int[] vehz = new int[5];
        int[] vehtgts = new int[] { -1, -1, -1, -1, -1 };

        int temp;

        public NavDecision(List<int> VehicleList, List<int> TargetList, NavMap[] navmaps, int[] vehiclex, int[] vehiclez)
        {
            VehList = new List<int>(VehicleList);
            TgtList = new List<int>(TargetList);
            Array.Copy(vehiclex, vehx, 5);
            Array.Copy(vehiclez, vehz, 5);

            BadValue = navmaps[0].getbadvalue();
            ShortestMaxDist = BadValue;
            VehicleListCount = VehList.Count;
            TgtListCount = TgtList.Count;
            NavDecisions = new NavDecision[VehicleListCount * TgtListCount];
            for (int i = 0; i < VehicleListCount; i++)
            {
                for (int j = 0; j < TgtListCount; j++)
                {
                    List<int> TgtListSub = new List<int>(TgtList);
                    TgtListSub.RemoveAt(j);
                    if (TgtListSub.Count > 0)
                    {
                        int[] tempvehx = new int[5];
                        int[] tempvehz = new int[5];
                        Array.Copy(vehx, tempvehx, 5);
                        Array.Copy(vehz, tempvehz, 5);
                        navmaps[TgtList.ElementAt(j)].getTargetPos(out tempvehx[VehicleList.ElementAt(i)], out vehiclez[VehicleList.ElementAt(i)]);
                        NavDecisions[i * TgtListCount + j] = new NavDecision(VehList, TgtListSub, navmaps, vehiclex, vehiclez);
                    }
                }
            }
        }

        private int getMinMaxRange(NavMap[] navmaps)
        {
            int result = BadValue;

            for (int i = 0; i < VehicleListCount; i++)
            {
                for (int j = 0; j < TgtListCount; j++)
                {
                    temp = navmaps[VehList.ElementAt(j)].getValue(vehx[TgtList.ElementAt(i)], vehz[TgtList.ElementAt(i)]);
                    if (temp < ShortestMaxDist)
                    {
                        ShortestMaxDist = temp;
                        VehicleNumIndex = i;
                        TgtNumIndex = j;
                    }
                }
            }
            return ShortestMaxDist;
        }

        public void updateTgts(NavMap[] navmaps, ref int[] vehicletgts)
        {
            int[] tempvehtgts = new int[5];
            Array.Copy(vehicletgts, tempvehtgts, 5);
            for (int i = 0; i < VehicleListCount; i++)
            {
                for (int j = 0; j < TgtListCount; j++)
                {
                    temp = navmaps[TgtList.ElementAt(j)].getValue(vehx[VehList.ElementAt(i)], vehz[VehList.ElementAt(i)]);
                    if (temp < ShortestMaxDist)
                    {
                        ShortestMaxDist = temp;
                        VehicleNumIndex = i;
                        TgtNumIndex = j;
                    }

                }
            }





                    if (VehicleNumIndex > -1)
                vehicletgts[VehList.ElementAt(VehicleNumIndex)] = TgtList.ElementAt(TgtNumIndex);
        }

    }*/
}
