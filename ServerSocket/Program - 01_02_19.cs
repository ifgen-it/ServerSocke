using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ServerSocket
{
    class Program
    {
        static int port = 8005;
        static string localHost = "127.0.0.1";

        static void Main(string[] args)
        {

            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(localHost), port);
            Socket listenSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listenSocket.Bind(ipPoint);
                listenSocket.Listen(10);
                Console.WriteLine("Server was started. Waiting for connections...");

                while (true)
                {
                    Socket handler = listenSocket.Accept();

                    // GET THE MESSAGE
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    byte[] data = new byte[256];

                    do
                    {
                        bytes = handler.Receive(data);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));

                    } while (handler.Available > 0);

                    Console.WriteLine(DateTime.Now.ToShortDateString() + ": " + builder.ToString());

                    // SEND THE ANSWER
                    string message = "your message delivered";
                    data = Encoding.Unicode.GetBytes(message);
                    handler.Send(data);

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

        }
    }
}
