using System;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace UdpClientProgram
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TEST
    {
        public string Buffer;
        public int     number;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string  aString;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] innerTestArray;
    }

    class Program
    {
        public const string password = "pass";
        static void Main(string[] args)
        {
            //Start ServerSide acknowledge
            byte[] resp = Encoding.ASCII.GetBytes("INIT");
            sendData(resp);

            while (true)
            {
                TEST test = new TEST();
                Console.WriteLine("");
                Console.WriteLine("ReadLine : ");
                test.aString = Console.ReadLine();
                test.number = 10;
                test.innerTestArray = null;
                test.Buffer = null;
                byte[] arr = structToBytes(test);
                sendData(arr);
            }
        }

        static void sendData(byte[] arr)
        {
            UdpClient client = new UdpClient();
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 600);
            client.Connect(ipep);

            //SEND BYTE[] TO SERVER
            client.Send(arr, arr.Length);
            var data = client.Receive(ref ipep);

            if (data[0] == 0x02)
            {
                data = data.Skip(1).ToArray();
                string token = Encoding.ASCII.GetString(data);
                string response = ComputeHash(token + password);
                byte[] resp = Encoding.ASCII.GetBytes(response);

                client.Send(resp, resp.Length);
                data = client.Receive(ref ipep);

                if (data[0] == 0x00)
                {
                    Console.WriteLine("Authentication Failed");
                }

                if (data[0] == 0x01)
                {
                    Console.WriteLine("Authentication Succeeded");
                }
            }

            //RESPONSE FROM SERVER
            if (data[0] == 0xFF)
            {
                data = data.Skip(1).ToArray();
            }

            client.Close();
        }

        static byte[] structToBytes(object str)
        {
            byte[] arr = new byte[Marshal.SizeOf(str)];
            IntPtr pnt = Marshal.AllocHGlobal(Marshal.SizeOf(str));
            Marshal.StructureToPtr(str, pnt, false);
            Marshal.Copy(pnt, arr, 0, Marshal.SizeOf(str));
            Marshal.FreeHGlobal(pnt);
            return arr;
        }

        static TEST structFromBytes(byte[] arr)
        {
            TEST str = new TEST();
            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(arr, 0, ptr, size);
            str = (TEST)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);
            return str;
        }

        public static String ComputeHash(String value)
        {
            using (SHA256 hash = SHA256Managed.Create())
            {
                return String.Join("", hash.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(item => item.ToString("x2")));
            }
        }
    }

}
