//#define REST
using System;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Numerics;
using Newtonsoft.Json;

using RestLike;
using System.Windows.Input;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RestForm
{
    [System.Serializable]
    public class ConfigTemplate
    {
        public string ipAddress;
        public int portNumber;
    }

    [System.Serializable]
    public class VehicleStateTemplate
    { 
        public int id;
        public bool isDynamic;
        public bool jammed;
        public bool cameraReady;
        public bool lidarReady;
        public int targetsLoaded;
        public float timeStamp;
        public float x;
        public float y;
        public float z;
        public float heading;
    }

    [System.Serializable]
    public class VehicleCommandTemplate
    {
        public float targetHeading;
        public float targetSpeed;
        public float targetAltitude;
    }

    [System.Serializable]
    public class TargetTemplate
    {
        public int remaining;
        public int recovered;
        public float[] latitudes;
        public float[] longitudes;
    }

    public class Program
    {
        private static int mapsize_x = 512;
        private static int mapsize_z = 512;
        private static int mapoffset_x = mapsize_x / 2;
        private static int mapoffset_z = mapsize_x / 2;
        private const int cameraImageSize = 512;
        private const int trackSize = 512;
        private const int lidarFrameSize = 7200;
        private const int numVehicles = 5;

        private static Form1 form;
        private static Bitmap trackBMP;
        private static Bitmap[] cameraBMPs;

        //variables for updatetrack to make it more effecient
        private static int updatetrack_bytes = 512 * 512 * 3;
        private static byte[] updatetrack_rgbValues = new byte[512 * 512 * 3];
        private static IntPtr updatetrack_ptr;
        private static Rectangle updatetrack_rect = new Rectangle(0, 0, 512, 512);
        private static System.Drawing.Imaging.BitmapData updatetrack_bmpData;

        private static ObjMap map = new ObjMap(mapsize_x, mapsize_z);
        private static List<NavMap> navmaps = new List<NavMap>();
        private static NavMap[] initnavmap = new NavMap[5];
        private static bool updatenavmaps = false;
        private static DroneNav[] dronenav = new DroneNav[5];
        private static int[] vehicletgt = new int[] { -1, -1, -1, -1, -1 };

        private static float[][] Dronewaypoints_x = new float[][]
        {
            new float[] { 0, 0, 10, 10, 20, 20, 30, 30, 40, 40, 50, 50, 60, 60, 70, 70, 80, 80, 90, 90, 100, 100, 110, 110, 120, 120, 130, 130, 140, 140, 150, 150, 160, 160, 170, 170, 180, 180, 190, 190, 200, 200, 210, 210, 220, 220, 230, 230, 240, 240, 250, 250 },
            new float[] { 0, 0, 10, 10, 20, 20, 30, 30, 40, 40, 50, 50, 60, 60, 70, 70, 80, 80, 90, 90, 100, 100, 110, 110, 120, 120, 130, 130, 140, 140, 150, 150, 160, 160, 170, 170, 180, 180, 190, 190, 200, 200, 210, 210, 220, 220, 230, 230, 240, 240, 250, 250 },
            new float[] {214,-214,-214,214,214,-214,-214,214,214,-214,-214,214,214,-214,-225,225,225},
            new float[] {-214,214,214,-214,-214,214,214,-214,-214,214,214,-214,-214,214},
            new float[] {214,-214,-214,214,214,-214,-214,214,214,-214,-214,214,214,-214,-225,-225,225}
        };
        private static float[][] Dronewaypoints_z = new float[][]
        {
            new float[] {-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10},
            new float[] {-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10,-10,260,260,-10},
            new float[] {-220,-220,-154,-154,-88,-88,-22,-22,44,44,110,110,176,176,     225, 225,-225},
            new float[] {-198,-198,-132,-132,-66,-66,0,0,66,66,132,132,198,198},
            new float[] {-176,-176,-110,-110,-44,-44,22,22,88,88,154,154,220,220,       225,-225,-225}
        };
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            File.Delete("forms.log");
            //initialise
            Initialise();

            //Set Up
            form = new Form1();
            form.SetUpFormControls(trackBMP, ref cameraBMPs);

            //launch challenge thread
            Thread challengeThread = new Thread(new ThreadStart(Program.Challenge));
            challengeThread.IsBackground = true;
            challengeThread.SetApartmentState(ApartmentState.STA);
            challengeThread.Start();
            Thread navEvalThread0 = new Thread(new ThreadStart(Program.NavEval0));
            Thread navEvalThread1 = new Thread(new ThreadStart(Program.NavEval1));
            Thread navEvalThreadi = new Thread(new ThreadStart(Program.NavEvali));
            navEvalThread0.IsBackground = true;
            navEvalThread0.SetApartmentState(ApartmentState.STA);
            navEvalThread0.Start();
            navEvalThread1.IsBackground = true;
            navEvalThread1.SetApartmentState(ApartmentState.STA);
            navEvalThread1.Start();
            navEvalThreadi.IsBackground = true;
            navEvalThreadi.SetApartmentState(ApartmentState.STA);
            navEvalThreadi.Start();

            //run form
            Application.Run(form);

            //Shutdown
            form.ShutDownFormControls();
            form.Dispose();
        }

        static void Initialise()
        {
            trackBMP = new Bitmap(trackSize, trackSize, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            cameraBMPs = new Bitmap[Form1.MaxCameras];
            for (int i = 0; i < cameraBMPs.Length; i++)
            {
                cameraBMPs[i] = new Bitmap(cameraImageSize, cameraImageSize, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            }
            for (int i = 0; i < 5; i++)
                dronenav[i] = new DroneNav(Dronewaypoints_x[i], Dronewaypoints_z[i]);
            initnavmap[0] = new NavMap(512, 512, -150f, -150f);
            initnavmap[1] = new NavMap(512, 512, 150f, 150f);
            initnavmap[2] = new NavMap(512, 512, 0f, 0f);
            initnavmap[3] = new NavMap(512, 512, 0f, 0f);
            initnavmap[4] = new NavMap(512, 512, 0f, 0f);
        }


        static int[] vehiclex = new int[] { 0, 0, 0, 0, 0 };
        static int[] vehiclez = new int[] { 0, 0, 0, 0, 0 };
        static float[] vehiclexf = new float[] { 0f, 0f, 0f, 0f, 0f };
        static float[] vehiclezf = new float[] { 0f, 0f, 0f, 0f, 0f };
        static float[] vehiclespeedx = new float[] { 0f, 0f, 0f, 0f, 0f };
        static float[] vehiclespeedz = new float[] { 0f, 0f, 0f, 0f, 0f };
        static float[][] vehicledirection = new float[][] { new float[] { 0, 0 },
                                                            new float[] { 0, 0 },
                                                            new float[] { 0, 0 },
                                                            new float[] { 0, 0 },
                                                            new float[] { 0, 0 }};
        static float[] vehiclespeed = new float[] { 0, 0, 0, 0, 0 };

        static float speedfactor = 250f;

        //need this to be outside the loop instead of instantiated as need it for speed calculations
        static long elapsedTime = 50;

        /// <summary>
        /// The challenge worker thread.
        /// </summary>
        [STAThread]
        static void Challenge()
        {
            int remainingTargets = 0;


            //load config json string
            StreamReader srConfig = new StreamReader("Config.json");
            string jsonConfigFromFile = srConfig.ReadToEnd();
            srConfig.Close();

            ConfigTemplate config = JsonConvert.DeserializeObject<ConfigTemplate>(jsonConfigFromFile);

            string baseEndpoint;
            string teamName;

            baseEndpoint = config.ipAddress + ":" + config.portNumber;
            teamName = "Team Brazier";

            while (!form.IsHandleCreated)
            {
                //skip
                Thread.Sleep(100);
            }
            //set up client
            form.message = "Attempting to connect to " + baseEndpoint;
            try
            {
                form.Invoke(form.imageDelegate);
            }
            catch (Exception e)
            {
                //drop out of thread
                string errorMessage = "form.Invoke failed, error: " + e.Message + "\n";
                File.AppendAllText("forms.log", errorMessage);
                return;
            }
            var client = new RestClient(baseEndpoint);
            //client setup
            form.message = "Connected to " + baseEndpoint;
            try
            {
                form.Invoke(form.imageDelegate);
            }
            catch (Exception e)
            {
                //drop out of thread
                string errorMessage = "form.Invoke failed, error: " + e.Message + "\n";
                File.AppendAllText("forms.log", errorMessage);
                return;
            }
            //post team name to register
            var requestTeam = new RestRequest("team", Method.Post);
            requestTeam.RequestFormat = DataFormat.String;
            requestTeam.AddBody(teamName);
            client.Execute(requestTeam);

            //post vehicle config from file
            //load vehicle config json string
            StreamReader sr = new StreamReader("Vehicles.json");
            string jsonFromFile = sr.ReadToEnd();
            sr.Close();

            var requestConfig = new RestRequest("vehicle/config", Method.Post);
            requestConfig.RequestFormat = DataFormat.Json;
            requestConfig.AddBody(jsonFromFile);
            client.Execute(requestConfig);

            //counter for mainloop
            long starttime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            //counter for the current loop time
            long looptime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            int loopno = 0;

            long targetFrameTime = 50;//default 100 milliseconds

            //keyboard control of vehicle 0
            float vehicle0Heading = 0f;
            //record position history - used for unstick function.
            int[][] vehiclepositionhistx = new int[][] {new int[]  {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                                        new int[]  {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                                        new int[]  {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                                        new int[]  {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                                        new int[]  {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}};
            int[][] vehiclepositionhistz = new int[][] {new int[]  {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                                        new int[]  {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                                        new int[]  {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                                        new int[]  {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                                                        new int[]  {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}};

            string LIDARstatemsg = "    ";
            float lidarmaxY = -9999f;
            float lidarminY = 9999f;

            RestResponse vehiclecommanddebug;

            //loop through vehicles till complete
            bool closing = false;
            while (!closing)
            {
                bool update = false;
                updatenavmaps = false;

                //record data transfer per frame
                long frameData = 0;

                //get remaing targets
                string resource = "team/targets";
                var requestTargets = new RestRequest(resource, Method.Get);
                var targets = client.Execute(requestTargets);
                if (targets != null && targets.valid && (targets.format == DataFormat.Json))
                {
                    //get body string
                    char[] bodydata = new char[targets.ContentLength];
                    Array.Copy(targets.RawBytes, 0, bodydata, 0, targets.ContentLength);
                    string json = new string(bodydata);
                    TargetTemplate targetData = JsonConvert.DeserializeObject<TargetTemplate>(json);
                    remainingTargets = targetData.remaining;
                }

                StartUpdateTrackImage();

                for (int i = 0; i < numVehicles; i++)
                {
                    //get current vehicle state from server
                    //vehicle ids in current json file are 0 to 4 so can use index i as id
                    resource = "vehicle/state?id=" + Convert.ToString(i);
                    var requestState = new RestRequest(resource, Method.Get);
                    var state = client.Execute(requestState);
                    VehicleStateTemplate vehicleState;
                    if ((state != null) && (state.ContentLength > 0) && (state.ContentLength != 17))
                    {
                        //get body string
                        char[] bodydata = new char[state.ContentLength];
                        Array.Copy(state.RawBytes, 0, bodydata, 0, state.ContentLength);
                        string json = new string(bodydata);
                        //if (json.StartsWith("vehicle/"))
                        //{
                        //    Console.WriteLine("Bad Json Vehicle Data");
                        //    continue;
                        //}
                        vehicleState = JsonConvert.DeserializeObject<VehicleStateTemplate>(json);

                        vehiclepositionhistx[i][loopno] = (int)vehicleState.x;
                        vehiclepositionhistz[i][loopno] = (int)vehicleState.z;

                        frameData += state.ContentLength;

                        if (vehicleState.isDynamic)
                        {
                            if (vehicleState.cameraReady)
                            {
                                //get Camera
                                resource = "vehicle/camera?id=" + Convert.ToString(i);
                                var requestCamera = new RestRequest(resource, Method.Get);
                                var camera = client.Execute(requestCamera);
                                if ((camera != null) && camera.valid)
                                {
                                    if (camera.format == DataFormat.Binary)
                                    {
                                        int offset = 0;
                                        float timestamp = BitConverter.ToSingle(camera.RawBytes, offset);
                                        offset += 4;
                                        int imageSize = BitConverter.ToInt32(camera.RawBytes, offset);
                                        offset += 4;
                                        //copy raw bytes from response to BMP image 
                                        Blit(camera.RawBytes, offset, ref cameraBMPs[i]);
                                        //update forms to display bitmap
                                        update = true;
                                        frameData += camera.ContentLength;
                                        if (i > 1 && milliseconds - starttime > 5000)
                                        {
                                            updatenavmaps |= Camera_Utils.checkTarget(vehicleState.x, vehicleState.z, vehicleState.heading,
                                                                     vehicleState.y, map, camera.RawBytes, navmaps);
                                        }
                                    }
                                    else
                                    {
                                        char[] messageData = new char[camera.ContentLength];
                                        Array.Copy(camera.RawBytes, 0, messageData, 0, camera.ContentLength);
                                        string error = new string(messageData);
                                    }
                                }
                            }

                            if (vehicleState.lidarReady)
                            {
                                //get LIDAR
                                resource = "vehicle/lidar?id=" + Convert.ToString(i);
                                var requestLidar = new RestRequest(resource, Method.Get);
                                var lidar = client.Execute(requestLidar);
                                if ((lidar != null) && lidar.valid)
                                {
                                    if (lidar.format == DataFormat.Binary)
                                    {
                                        //lidar valid
                                        int offset = 0;
                                        float timestamp = BitConverter.ToSingle(lidar.RawBytes, offset);
                                        offset += 4;
                                        int pointCount = BitConverter.ToInt32(lidar.RawBytes, offset);
                                        offset += 4;
                                        //run through lidar points
                                        //this code doesn't do anything it is just an example of retrieving the point cloud from binary data
                                        Vector3 lidarPoint = Vector3.Zero;
                                        for (int pointOffset = 0; pointOffset < (pointCount * 12); pointOffset += 12)//lidar.RawBytes.Length - 19
                                        {
                                            lidarPoint.X = BitConverter.ToSingle(lidar.RawBytes, offset + pointOffset);
                                            lidarPoint.Y = BitConverter.ToSingle(lidar.RawBytes, offset + pointOffset + 4);
                                            lidarPoint.Z = BitConverter.ToSingle(lidar.RawBytes, offset + pointOffset + 8);
                                            if ((lidarPoint.Y > 0.15) &&
                                                Camera_Utils.checkNoVehicleInLidar(i, vehiclex, vehiclez, lidarPoint.X, lidarPoint.Z))
                                            {
                                                //UpdateTrack(lidarPoint.X, lidarPoint.Z, true);
                                                updatenavmaps |= map.setObstacle(lidarPoint.X, lidarPoint.Z);
                                            } else if ((lidarPoint.Y < 0))
                                            {
                                                map.setSeen(lidarPoint.X, lidarPoint.Z);
                                            }
                                            if (lidarPoint.Y < lidarminY) lidarminY = lidarPoint.Y;
                                            if (lidarPoint.Y > lidarmaxY) lidarmaxY = lidarPoint.Y;
                                        }
                                        //don't think i'm going to need this, will use the point data another way
                                        //frameData += lidar.ContentLength;
                                        LIDARstatemsg = "OK  ";
                                    }
                                    else
                                    {
                                        char[] messageData = new char[lidar.ContentLength];
                                        Array.Copy(lidar.RawBytes, 0, messageData, 0, lidar.ContentLength);
                                        string error = new string(messageData);
                                        LIDARstatemsg = "BAD1";
                                    }
                                }
                            }
                            else
                            {
                                LIDARstatemsg = "NRDY";
                            }

                            UpdateTrack(vehicleState.x, vehicleState.z, false);

                            //post command
                            float targetSpeed = 20f; //4f + i * 4;
                            float targetAltitude = 12f;
                            float targetHeading = 0f;
                            /*if ((looptime % 100000) > 50000)
                            {
                                targetHeading = 90f;
                            }*/

                            //get vehicle speeds, useful
                            vehiclespeedx[i] = speedfactor * (vehicleState.x - vehiclexf[i]) / (float)elapsedTime; //used to use targetFrameTime hoping real time is better
                            vehiclespeedz[i] = speedfactor * (vehicleState.z - vehiclezf[i]) / (float)elapsedTime;

                            //keyboard control of vehicle 0
                            if (i == 0 || i == 1)
                            {
                                //test keyboard control
                                targetSpeed = 0f;

                                //unstick code
                                if (loopno < 20 &&
                                       !vehiclepositionhistx[i].Any(anyval => anyval != (int)vehicleState.x) &&
                                       !vehiclepositionhistz[i].Any(anyval => anyval != (int)vehicleState.z))
                                {
                                    if (loopno == 0)
                                    {
                                        Random rand = new Random();
                                        vehicle0Heading = (float)rand.NextDouble() * 360f;
                                    }
                                    targetSpeed = -10f;
                                }
                                //normal navigation code
                                else
                                {
                                    //testing adding this to its own thread
                                    //if (loopno == 0)
                                    //    updatenavmaps |= navmaps.ElementAt(0).EvalRoute(map, vehicleState.x, vehicleState.z);
                                    try //needs to be a try as if the element is deleted must catch.
                                    {
                                        if ((navmaps.Count > 0) && (vehicletgt[i] > -1))
                                            vehicle0Heading = navmaps.ElementAt(vehicletgt[i]).getDirection(vehicleState.x + vehiclespeedx[i], vehicleState.z + vehiclespeedz[i]);
                                        //turn off navigation to home locations for the sync swim map as its less effecient.
                                        else
                                            vehicle0Heading = initnavmap[i].getDirection(vehicleState.x + vehiclespeedx[i], vehicleState.z + vehiclespeedz[i]);
                                    }
                                    catch
                                    {
                                        Console.WriteLine("vehicle heading set error, caught, continuing");
                                        vehicle0Heading = 720;
                                    }
                                    if (vehicle0Heading < 720)
                                    {
                                        if (vehicledirection[i][0] == vehicle0Heading || vehicledirection[i][1] == vehicle0Heading)
                                        {
                                            //vehiclespeed[i] = vehiclespeed[i] + 0.5f;
                                            //targetSpeed = vehiclespeed[i];
                                            //added for sync swim only may need to remove
                                            //if (vehicletgt[i] > -1)
                                            targetSpeed = 10f;
                                        }
                                        else
                                        {
                                            targetSpeed = 0;
                                            vehiclespeed[i] = 0;
                                        }
                                        vehicledirection[i][1] = vehicledirection[i][0];
                                        vehicledirection[i][0] = vehicle0Heading;
                                    }
                                }

                                if (i == 1)
                                {
                                    if (Keyboard.IsKeyDown(Key.Left))
                                    {
                                        vehicle0Heading = 270;
                                        targetSpeed = 10;
                                        //vehicle0Heading -= 90;
                                    }
                                    if (Keyboard.IsKeyDown(Key.Right))
                                    {
                                        vehicle0Heading = 90;
                                        targetSpeed = 10;
                                        //vehicle0Heading += 90;
                                    }
                                    if (Keyboard.IsKeyDown(Key.Up))
                                    {
                                        vehicle0Heading = 0;
                                        targetSpeed = 10;
                                    }
                                    if (Keyboard.IsKeyDown(Key.Down))
                                    {
                                        vehicle0Heading = 180;
                                        targetSpeed = 10;
                                        //targetSpeed = -10;
                                    }
                                }



                                targetHeading = vehicle0Heading;

                                updatenavmaps |= map.setVisited(vehicleState.x, vehicleState.z);
                            }
                            else
                            {
                                targetHeading = dronenav[i].getHeading(vehicleState.x, vehicleState.z);
                                targetSpeed = dronenav[i].getSpeed(vehicleState.x, vehicleState.z, vehiclespeedx[i], vehiclespeedz[i]);

                                if (i == 5) //set this to a valid number if desired - this is for drone navigation data
                                {
                                    //Console.WriteLine("x: " + vehicleState.x + " z: " + vehicleState.z + dronenav[i].getwaypointstring());
                                    Console.WriteLine("xv: " + vehiclespeedx[i].ToString() + " zv: " + vehiclespeedz[i].ToString());
                                }
                            }

                            vehiclex[i] = (int)vehicleState.x + mapoffset_x;
                            vehiclez[i] = (int)vehicleState.z + mapoffset_z;
                            vehiclexf[i] = vehicleState.x;
                            vehiclezf[i] = vehicleState.z;

                            resource = "vehicle/command?id=" + Convert.ToString(i);
                            VehicleCommandTemplate command = new VehicleCommandTemplate();
                            command.targetHeading = targetHeading;
                            command.targetSpeed = targetSpeed;
                            command.targetAltitude = targetAltitude;
                            string commandJson = JsonConvert.SerializeObject(command);
                            var requestCommand = new RestRequest(resource, Method.Post);
                            requestCommand.RequestFormat = DataFormat.Json;
                            requestCommand.AddBody(commandJson);
                            vehiclecommanddebug = client.Execute(requestCommand);
                        }
                    }


                }

                EndUpdateTrackImage(loopno == 0); // go to next navmap if the timer is out

                //Copy currently selected image to screen if updated
                if (update)
                {
                    if (!form.IsHandleCreated)
                    {
                        //skip
                        closing = true;
                    }
                    else
                    {
                        try
                        {
                            form.Invoke(form.imageDelegate);
                        }
                        catch (Exception e)
                        {
                            string errorMessage = "form.Invoke failed, error: " + e.Message + "\n";
                            File.AppendAllText("forms.log", errorMessage);
                            closing = true;
                        }
                    }
                }

                initnavmap[0].setTargetPos(-180, vehiclezf[3] + 50);
                initnavmap[1].setTargetPos(180, vehiclezf[3] + 50);

                updatenavmaps = true;

                if (updatenavmaps && navmaps.Count > 0)
                {
                    try
                    {
                        int badvalue = navmaps[0].getbadvalue();
                        NavMap.checkNavmaplist(map, navmaps);
                        int navmapcount = navmaps.Count;
                        /*bool notdone = true;
                        int trucks = 2;

                        int[] tgtindexes = new int[navmapcount];
                        int tgtindexlen = 0;
                        
                        
                        int[,] tgtdistances = new int[trucks, navmapcount];

                        bool[] trucksdone = { false, false, true, true, true }; //drones are done as don't need targets.

                        for (int i = 0; i < trucks; i++)
                        {
                            for (int j = 0; j < navmapcount; j++)
                            {
                                if (!trucksdone[i])
                                {
                                    tgtdistances[i, j] = navmaps[j].getValue(vehiclex[i], vehiclez[i]);
                                }
                                else
                                {
                                    tgtdistances[i, j] = badvalue;
                                }
                            }
                        }*/

                        int temp0 = badvalue;
                        int temp1 = badvalue;
                        int temp  = badvalue * 5;
                        //int veh0tgt = -1;
                        //int veh1tgt = -1;
                        int shortest = temp;

                        for (int i = -1; i < navmapcount; i++)
                        {
                            for (int j = -1; j < navmapcount; j++)
                            {
                                if (i != j)
                                {
                                    if (i > -1)
                                        temp0 = navmaps[i].getValue(vehiclex[0], vehiclez[0]);
                                    else
                                        temp0 = badvalue;
                                    if (j > -1)
                                        temp1 = navmaps[j].getValue(vehiclex[1], vehiclez[1]);
                                    else
                                        temp1 = badvalue;

                                    temp = temp0 + temp1;
                                    if (temp < shortest)
                                    {
                                        vehicletgt[0] = i;
                                        vehicletgt[1] = j;
                                        shortest = temp;
                                    }

                                }
                            }
                        }


                        /*
                        while (notdone)
                        {
                            int k = -1;
                            int minval = badvalue;
                            for (int i = 0; i < tgtdistances.Count(); i++)
                            {
                                if (tgtdistances[i] < minval)
                                {
                                    k = i;
                                    minval = tgtdistances[i];
                                }
                            }
                            //int k = tgtdistances.Select((x, i) => (x, i).Min());
                            if (k > -1)
                            {
                                int elimvehicleindex = ((k - k % navmapcount) / navmapcount);
                                int tgtindex = (k % navmapcount);
                                for (int j = elimvehicleindex * navmapcount; j < (elimvehicleindex + 1) * navmapcount; j++)
                                {
                                    tgtdistances[j] = badvalue;
                                }
                                for (int i = 0; i < 5; i++)
                                {
                                    tgtdistances[navmapcount * i + tgtindex] = badvalue;
                                }
                                vehicletgt[elimvehicleindex] = tgtindex;
                                trucksdone[elimvehicleindex] = true;
                                notdone = !trucksdone.All(val => val == true);
                            }
                            else
                            {
                                for (int i = 0; i < 5; i++)
                                {
                                    if (trucksdone[i] == false)
                                        vehicletgt[i] = -1;
                                }
                                notdone = false;
                            }
                        }*/
                    }
                    catch
                    {
                        Console.WriteLine("navmap update exception caught, continuing");
                    }

                    //old method for prioritising targets, not very effecient.
                    /*int[] lastruckrange = new int[] { badvalue, badvalue, badvalue, badvalue, badvalue };
                    int[] truckrange = new int[] { 0, 0, 0, 0, 0 };
                    vehicletgt = new int[] { -1, -1, -1, -1, -1 };
                    while (notdone)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            try
                            {
                                for (int j = 0; j < navmaps.Count; j++)
                                {
                                    truckrange[i] = navmaps[j].getValue(vehiclex[i], vehiclez[i]);
                                    if ((truckrange[i] < lastruckrange[i]) &&
                                            !vehicletgt.Any(val => val == j))
                                    {
                                        vehicletgt[i] = j;
                                        lastruckrange[i] = truckrange[i];
                                    }
                                }
                                notdone = false;
                            } catch { Console.WriteLine("updatenavmaps error, caught, continuing..."); }
                        }

                    }*/
                }

                //stabilise loop time
                looptime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                elapsedTime = looptime - milliseconds;
                if (elapsedTime < targetFrameTime)
                {
                    Thread.Sleep((int)(targetFrameTime - elapsedTime));
                }
                looptime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                elapsedTime = looptime - milliseconds;
                milliseconds = looptime;
                string message = "Remaining Targets: " + Convert.ToString(remainingTargets) + ",  Frame time: " + Convert.ToString(elapsedTime) + ",  Frame data: " + Convert.ToString(frameData) + ", LIDAR STATE: " + LIDARstatemsg + ", LIDAR MIN/MAX: " + Convert.ToString(lidarminY) + " " + Convert.ToString(lidarmaxY);
                form.message = message;

                loopno = (loopno + 1) % 40;
            }
        }

        static void Blit(Byte[] source, int offset, ref Bitmap bmp)
        {
            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmp.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbValues = new byte[bytes];

            //// Copy the RGB values into the array.
            //System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            //copy lines from source 
            for (int height = 0; height < bmp.Height; height++)
            {
                int sourceOffset = offset + (bmp.Height - height - 1) * bmp.Width * 3;//3 bytes per pixel invert for bmp
                int destOffset = height * bmpData.Stride;
                Array.Copy(source, sourceOffset, rgbValues, destOffset, bmp.Width * 3);

            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);

            // Unlock the bits.
            bmp.UnlockBits(bmpData);
        }


        static void UpdateTrack(float x, float z, bool obstacle)
        {
            /* Now in StartUpdateTrackImage
            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, 512, 512);
            System.Drawing.Imaging.BitmapData bmpData =
                trackBMP.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                trackBMP.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            commented this out as these variables are now static and global, huge performance increase
            // Declare an array to hold the bytes of the bittrack.
            //int bytes = Math.Abs(bmpData.Stride) * trackBMP.Height;
            //byte[] rgbValues = new byte[bytes];
            

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, updatetrack_rgbValues, 0, updatetrack_bytes);
            */

            // add point at current location
            if ((x > -250f) && (x < 250f))
            {
                if ((z > -250f) && (z < 250f))
                {
                    //add 3d point mapping 1m to 1 pixel centre of bitmap a 0,0
                    int w = (int)(x) + trackBMP.Width / 2;
                    int h = -(int)(z) + trackBMP.Height / 2;//bitmap inverted
                    int offset = h * updatetrack_bmpData.Stride + w * 3;
                    byte red = 0, green = 255, blue = 0;
                    if (!obstacle)
                    {
                        red = 255; green = 255; blue = 255;
                    }
                    updatetrack_rgbValues[offset] = blue;
                    updatetrack_rgbValues[offset + 1] = green;
                    updatetrack_rgbValues[offset + 2] = red;
                }
            }

            /* now in EndUpdateTrackImage
            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(updatetrack_rgbValues, 0, ptr, updatetrack_bytes);

            // Unlock the bits.
            trackBMP.UnlockBits(bmpData);
            */
        }

        static void StartUpdateTrackImage()
        {
            // Lock the bitmap's bits.  
            updatetrack_rect = new Rectangle(0, 0, 512, 512);
            updatetrack_bmpData =
                trackBMP.LockBits(updatetrack_rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                trackBMP.PixelFormat);

            // Get the address of the first line.
            updatetrack_ptr = updatetrack_bmpData.Scan0;

            System.Runtime.InteropServices.Marshal.Copy(updatetrack_ptr, updatetrack_rgbValues, 0, updatetrack_bytes);
        }

        static int drawnavmapi = 0;
        static void EndUpdateTrackImage(bool nextmap)
        {
            bool newmapmode = true;
            if (newmapmode) // false t
            {
                if (nextmap)
                {
                    if (drawnavmapi < (navmaps.Count - 1))
                        drawnavmapi++;
                    else
                        drawnavmapi = 0;
                }
                for (int x = 0; x < 512; x++)
                {
                    for (int z = 0; z < 512; z++)
                    {
                        //add 3d point mapping 1m to 1 pixel centre of bitmap a 0,0
                        int offset = z * updatetrack_bmpData.Stride + x * 3;
                        byte red = 0, green = 255, blue = 0;
                        if (map.getVoxel(x, 511 - z) == T_map_voxel.Obstacle)
                        {
                            red = 180; green = 180; blue = 180;
                        }
                        else if (map.getVoxel(x, 511 - z) == T_map_voxel.Target)
                        {
                            red = 255; green = 50; blue = 50;
                        }
                        else if (map.getVoxel(x, 511 - z) == T_map_voxel.Visited)
                        {
                            red = 0; green = 0; blue = 255;
                        }
                        else
                        {
                            red = 0; green = 0; blue = 0;
                            if (drawnavmapi < navmaps.Count)
                            {
                                navmaps.ElementAt(drawnavmapi).getColor(ref red, ref green, ref blue, x, 511 - z);
                            }
                        }
                        updatetrack_rgbValues[offset] = blue;
                        updatetrack_rgbValues[offset + 1] = green;
                        updatetrack_rgbValues[offset + 2] = red;
                    }
                }
                for (int i = 0; i < 5; i++)
                {
                    int offset;
                    int x = vehiclex[i];
                    int z = 511 - (vehiclez[i]);
                    if ((x > + 2) && (x < mapsize_x - 3) && (z > + 2) && (z < mapsize_z - 3))
                    {
                        for (int j = -2; j < 3; j++)
                        {
                            offset = (z - 2) * updatetrack_bmpData.Stride + (x + j) * 3;
                            updatetrack_rgbValues[offset] = 255;
                            updatetrack_rgbValues[offset + 1] = 255;
                            updatetrack_rgbValues[offset + 2] = 255;

                            offset = (z + 2) * updatetrack_bmpData.Stride + (x + j) * 3;
                            updatetrack_rgbValues[offset] = 255;
                            updatetrack_rgbValues[offset + 1] = 255;
                            updatetrack_rgbValues[offset + 2] = 255;

                            offset = (z + j) * updatetrack_bmpData.Stride + (x + 2) * 3;
                            updatetrack_rgbValues[offset] = 255;
                            updatetrack_rgbValues[offset + 1] = 255;
                            updatetrack_rgbValues[offset + 2] = 255;

                            offset = (z + j) * updatetrack_bmpData.Stride + (x - 2) * 3;
                            updatetrack_rgbValues[offset] = 255;
                            updatetrack_rgbValues[offset + 1] = 255;
                            updatetrack_rgbValues[offset + 2] = 255;

                        }
                    }
                }
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(updatetrack_rgbValues, 0, updatetrack_ptr, updatetrack_bytes);

            // Unlock the bits.
            trackBMP.UnlockBits(updatetrack_bmpData);

        }

        //disabled naveval0 thread, only navevali is active.
        static void NavEval0()//int vehicleID)
        {
            long navlooptime = 0;
            long starttime;
            int vehicleID = 0;
            while (false)
            {
                starttime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                try
                    {
                        if (vehicletgt[vehicleID] > -1)
                            updatenavmaps |= navmaps[vehicletgt[vehicleID]].EvalRoute(map, vehiclex[vehicleID], vehiclez[vehicleID]);
                        else
                            initnavmap[vehicleID].EvalRoute(map, vehiclex[vehicleID], vehiclez[vehicleID], true);
                    }
                catch
                    {
                        Console.WriteLine("Null Exception in NavEval (vehiclethread), caught, continuing...");
                        updatenavmaps = true;
                    }
                navlooptime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - starttime;
                Console.WriteLine("NavEval0 time : " + navlooptime.ToString());
            }
            

        }
        //disabled naveval1 thread, only navevali is active.
        static void NavEval1()//int vehicleID)
        {
            long navlooptime = 0;
            long starttime;
            int vehicleID = 1;
            while (false)
            {
                starttime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                try
                    {
                        if (vehicletgt[vehicleID] > -1)
                            updatenavmaps |= navmaps[vehicletgt[vehicleID]].EvalRoute(map, vehiclex[vehicleID], vehiclez[vehicleID]);
                        else
                            initnavmap[vehicleID].EvalRoute(map, vehiclex[vehicleID], vehiclez[vehicleID], true);
                    }
                catch
                    {
                        Console.WriteLine("Null Exception in NavEval (vehiclethread), caught, continuing...");
                        updatenavmaps = true;
                    }
                navlooptime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - starttime;
                Console.WriteLine("NavEval1 time : " + navlooptime.ToString());
            }

        }
        static void NavEvali()
        {
            long navlooptime = 0;
            long starttime;
            while (true)
            {
                for (int i = 0; i < navmaps.Count; i++)
                {
                    if (true)//(!vehicletgt.Any(anyval => anyval == i))
                    {
                        starttime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        try
                        {
                            updatenavmaps |= navmaps[i].EvalRoute(map, vehiclex[0], vehiclez[0]);
                        }
                        catch
                        {
                            Console.WriteLine("Null Exception in NavEval (idlethread), caught, continuing...");
                            updatenavmaps = true;
                        }
                        navlooptime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - starttime;
                        Console.WriteLine("NavEvali time : " + navlooptime.ToString());
                    }
                }
                initnavmap[0].EvalRoute(map, vehiclex[0], vehiclez[0], true);
                initnavmap[1].EvalRoute(map, vehiclex[1], vehiclez[1], true);
            }
        }
    }
}
