using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RestLike
{

    class RestClient
    {
        Socket client;
        byte[] sendbuf;
        byte[] recvbuf;

        //set up a client socket to handle requests
        public RestClient(string baseEndPoint)
        {
            sendbuf = new byte[1024];
            recvbuf = new byte[8 + 786432 + 8];//size of camera image + 8 byte header + 8 byte tail

            File.Delete("comms.log");

            var parts = baseEndPoint.Split(':');
            if (parts.Length == 2)
            {
                IPAddress address = IPAddress.Parse(parts[0]);
                int port = Convert.ToInt32(parts[1]);
                IPEndPoint ipEndPoint = new IPEndPoint(address, port);

                client = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                bool connected = false;
                while (!connected)
                {
                    try
                    {
                        client.Connect(ipEndPoint);
                        connected = true;
                    }
                    catch (SocketException e)
                    {
                        connected = false;
                        string errorMessage = "RestClient failed to connect, error: " + e.Message + "\n";
                        File.AppendAllText("comms.log", errorMessage);
                        Thread.Sleep(100);
                    }
                }
            }

        }

        public RestResponse Execute(RestRequest request)
        {
            RestResponse response = null; ;

            if (request.Method == Method.Post)
            {
                try
                {
                    response = Post(request);
                }
                catch (SocketException e)
                {
                    string errorMessage = "RestClient post failed, error: " + e.Message + "\n";
                    File.AppendAllText("comms.log", errorMessage);
                }
            }
            else
            {
                try
                {
                    response = Get(request);
                }
                catch (SocketException e)
                {
                    string errorMessage = "RestClient get failed, error: " + e.Message + "\n";
                    File.AppendAllText("comms.log", errorMessage);
                }
            }
            return response;
        }

        public RestResponse Post(RestRequest request)
        {
            RestResponse response = new RestResponse();
            int offset = 0;
            //message type
            Byte method = (Byte)request.Method;
            sendbuf[offset] = method;
            offset++;
            //message format
            Byte format = (Byte)request.RequestFormat;
            sendbuf[offset] = format;
            offset++;
            //message request length
            int length = request.Resource.Length;
            byte[] resourceLengthBytes = BitConverter.GetBytes(length);
            Array.Copy(resourceLengthBytes, 0, sendbuf, offset, resourceLengthBytes.Length);
            offset += resourceLengthBytes.Length;
            //message request
            length = Encoding.UTF8.GetBytes(request.Resource, 0, request.Resource.Length, sendbuf, offset);
            offset += request.Resource.Length;
            //message body length
            length = request.Body.Length;
            byte[] bodyLengthBytes = BitConverter.GetBytes(length);
            Array.Copy(bodyLengthBytes, 0, sendbuf, offset, bodyLengthBytes.Length);
            offset += bodyLengthBytes.Length;
            //message body
            length = Encoding.UTF8.GetBytes(request.Body, 0, request.Body.Length, sendbuf, offset);
            offset += request.Body.Length;

            client.Send(sendbuf, offset, SocketFlags.None);

            var received = client.Receive(recvbuf, SocketFlags.None);
            response.ContentLength = received;
            response.RawBytes = recvbuf;

            return response;
        }

        public RestResponse Get(RestRequest request)
        {
            RestResponse response = new RestResponse();
            int offset = 0;
            //message type
            Byte method = (Byte)request.Method;
            sendbuf[offset] = method;
            offset++;
            //message format
            Byte format = (Byte)request.RequestFormat;
            sendbuf[offset] = format;
            offset++;
            //message request length
            int length = request.Resource.Length;
            byte[] resourceLengthBytes = BitConverter.GetBytes(length);
            Array.Copy(resourceLengthBytes, 0, sendbuf, offset, resourceLengthBytes.Length);
            offset += resourceLengthBytes.Length;
            //message request
            length = Encoding.UTF8.GetBytes(request.Resource, 0, request.Resource.Length, sendbuf, offset);
            offset += request.Resource.Length;

            client.Send(sendbuf, offset, SocketFlags.None);

            var received = client.Receive(recvbuf, SocketFlags.None);
            if (received < 8)
            {
                response.valid = false;
                response.format = RestLike.DataFormat.None;
                response.ContentLength = received;
                response.RawBytes = recvbuf;
            }
            else
            {
                response.ContentLength = received - 8;
                int headerOffset = received - 8;
                response.RawBytes = recvbuf;
                response.valid = recvbuf[headerOffset] == 1;
                response.format = (RestLike.DataFormat)recvbuf[headerOffset + 1];
                int datalength = BitConverter.ToInt32(recvbuf, headerOffset + 4);
                if ((response.valid) && (datalength != response.ContentLength))
                {
                    response.valid = false;
                }
            }
            return response;
        }


    }
    class RestRequest
    {
        
        public Method Method;
        public String Resource;
        public DataFormat RequestFormat;
        public String Body;

        public RestRequest(string resource, Method method)
        {
            Method = method;
            Resource = resource;
        }

        public void AddBody(string body)
        {
            Body = body;
        }
    }

    class RestResponse
    {
        public bool valid = false;
        public DataFormat format = DataFormat.None;
        public int ContentLength = 0;
        public byte[] RawBytes;
    }

    public enum Method
    {
        Get = 0,
        Post = 1,
        Put = 2,
        Delete = 3,
        Head = 4,
        Options = 5,
        Patch = 6,
        Merge = 7,
        Copy = 8,
        Search = 9
    }

    public enum DataFormat
    {
        Json = 0,
        String = 1,
        Binary = 2,
        None = 3
    }
}
