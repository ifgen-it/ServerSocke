using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerSocket
{
    class Program
    {
        static int port = 8005;
        static string localHost = "127.0.0.1";

        static string getHistory = "0";
        static string wantExit = "2";
        static string whoIsOnline = "1";
        static string messageDelivered = "+";

        static double socketTimeoutInMinutes = 5;
        static int clientsLimitForListener = 10;


        static int clientsCounter = 0;
        static Socket listenSocket;
        static bool socketFound = false;
        static Socket foundSocket;
        static List<Socket> socketList = null;
        static List<string> bufferList = null;
        static List<string> namesList = null;
        static List<bool> getNameList = null;
        static List<DateTime> socketTimer = null;

        static void Main(string[] args)
        {

            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(localHost), port);
            listenSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listenSocket.Bind(ipPoint);
                listenSocket.Listen(clientsLimitForListener);
                Console.WriteLine("Server was started. Waiting for connections...\n");

                Thread threadListener = new Thread(socketListener);

                threadListener.Start();
                socketList = new List<Socket>();
                bufferList = new List<string>();
                namesList = new List<string>();
                getNameList = new List<bool>();
                socketTimer = new List<DateTime>();

                while (true)
                {
                    Thread.Sleep(500);
                    if (socketFound)
                    {
                        socketList.Add(foundSocket);
                        bufferList.Add("");
                        namesList.Add("");
                        getNameList.Add(false);
                        socketTimer.Add(DateTime.Now);

                        foundSocket = null;
                        socketFound = false;
                    }

                    if (socketList.Count > 0)
                    {
                        // CHECKING FOR DISCONNECTED CLIENTS

                        bool wasDeleted = false;
                        while (true)
                        {
                            wasDeleted = false;
                            int socketsNumber = socketList.Count;
                            for (int i = 0; i < socketsNumber; i++)
                            {
                                DateTime currTime = DateTime.Now;
                                DateTime timeLimit = socketTimer[i].AddMinutes(socketTimeoutInMinutes);

                                //Console.WriteLine("time from last activity: " + (currTime - socketTimer[i]));
                                if (currTime > timeLimit)
                                {
                                    wasDeleted = true;
                                    --clientsCounter;
                                    Console.WriteLine("\n"+ DateTime.Now.ToLongTimeString() + " " + namesList[i] + " was disconnected. Timeout: " + socketTimeoutInMinutes + " mins. Number of clients: " + clientsCounter);
                                    socketList[i].Shutdown(SocketShutdown.Both);
                                    socketList[i].Close();

                                    socketList.RemoveAt(i);
                                    bufferList.RemoveAt(i);
                                    namesList.RemoveAt(i);
                                    getNameList.RemoveAt(i);
                                    socketTimer.RemoveAt(i);
                                    break;
                                }
                            }
                            if (wasDeleted == false) break;
                        }

                        // GET MESSAGE FROM ALL CLIENTS -- TRAVERSING ALL SOCKETS
                        for (int i = 0; i < socketList.Count; i++)
                        {
                            if (socketList[i].Available <= 0) continue;

                            // GET THE MESSAGE
                            StringBuilder builder = new StringBuilder();
                            int bytes = 0;
                            byte[] data = new byte[256];
                            do
                            {
                                bytes = socketList[i].Receive(data);
                                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                            } while (socketList[i].Available > 0);

                            socketTimer[i] = DateTime.Now;

                            // CLIENT JUST CONNECTED - WRITES HIS NAME
                            if (getNameList[i] == false)
                            {
                                namesList[i] = builder.ToString().Trim();
                                getNameList[i] = true;
                                string answer = namesList[i] + " was connected to the chat";
                                Console.WriteLine("\n" + DateTime.Now.ToLongTimeString() + " " + answer + ". Number of clients: " + clientsCounter + "\n");

                                data = Encoding.Unicode.GetBytes(answer);
                                socketList[i].Send(data);
                                continue;
                            }

                            // CLIENT WANT TO UPDATE HISTORY
                            if (builder.ToString().Equals(getHistory))
                            {
                                        
                                // SEND THE ANSWER
                                string historyMess = "+";
                                // ADD HISTORY FROM OTHERS
                                if (!bufferList[i].Equals(""))
                                {
                                    historyMess += /*"\n\nnews:*/ "\n" +  bufferList[i];
                                    bufferList[i] = "";
                                }
                                data = Encoding.Unicode.GetBytes(historyMess);
                                socketList[i].Send(data);
                                continue;
                            }

                            // CLIENT WANT GET ONLINE USERS
                            if (builder.ToString().Equals(whoIsOnline))
                            {

                                // SEND THE ANSWER
                                string onlineUsers = messageDelivered + "\n";

                                for (int u = 0; u < namesList.Count; u++)
                                {
                                    onlineUsers += namesList[u];
                                    if (u < namesList.Count - 1)
                                    {
                                        //onlineUsers += "\n";
                                        onlineUsers += new StringBuilder().AppendLine().ToString();
                                    }
                                }

                                data = Encoding.Unicode.GetBytes(onlineUsers);
                                socketList[i].Send(data);
                                continue;
                            }

                            // CLIENT EXITED FROM CHAT 
                            if (builder.ToString().Equals(wantExit))
                            {
                                --clientsCounter;
                                Console.WriteLine("\n" + DateTime.Now.ToLongTimeString() + " " + namesList[i] + " was exited from the chat. Number of clients: " + clientsCounter);
                                socketList[i].Shutdown(SocketShutdown.Both);
                                socketList[i].Close();

                                socketList.RemoveAt(i);
                                bufferList.RemoveAt(i);
                                namesList.RemoveAt(i);
                                getNameList.RemoveAt(i);
                                socketTimer.RemoveAt(i);
                                break;
                            }


                            string textRes = DateTime.Now.ToLongTimeString() + " " + namesList[i] + " > " + builder.ToString();
                            Console.WriteLine(textRes);

                            //UPDATE BUFFER FOR OTHERS
                            for (int j = 0; j < socketList.Count; j++)
                            {
                                if (j == i) continue;
                                bufferList[j] += textRes + NewLine();
                            }

                            // SEND THE ANSWER
                            string message = messageDelivered;
                            // ADD HISTORY FROM OTHERS
                            if (!bufferList[i].Equals(""))
                            {
                                message += /*"\n\nnews:*/ "\n" + bufferList[i];
                                bufferList[i] = "";
                            }
                            data = Encoding.Unicode.GetBytes(message);
                            socketList[i].Send(data);
                        }

                        //handler.Shutdown(SocketShutdown.Both);
                        //handler.Close();
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Server finished");
            Console.ReadKey();

        }

        private static void socketListener()
        {
            while (true)
            {
                Socket handler = listenSocket.Accept();
                clientsCounter++;
                socketFound = true;
                foundSocket = handler;
                Thread.Sleep(1000);
            }
        }

        private static string NewLine()
        {
            return new StringBuilder().AppendLine().ToString();
        }
    }
}
