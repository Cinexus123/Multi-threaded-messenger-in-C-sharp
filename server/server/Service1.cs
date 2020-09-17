using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public partial class Service1 : ServiceBase
    {
        Thread t1;
        Thread t2;
        IPAddress ipAddress;
        IPEndPoint localEndPoint;
        Socket socketServer;
        public ConcurrentQueue<SocketInformation> bytesMessage = new ConcurrentQueue<SocketInformation>();
        List<SocketInformation> addPendingInformation = new List<SocketInformation>();

        Boolean flag = true;
        int rower = 0;
        static readonly object semaphore = new object();

        public Service1()
        {
            InitializeComponent();

            ipAddress = System.Net.IPAddress.Parse("127.0.0.1");
            localEndPoint = new IPEndPoint(ipAddress, 8000);
            socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        protected override void OnStart(string[] args)
        {
            socketServer = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socketServer.Bind(localEndPoint);
            socketServer.Listen(1);

            t1 = new Thread(() =>
            {
                while (flag)
                {
                    GenerateNewUserConnection();
                }
            });
            t1.Start();
        }

        private void CheckIfUserHasIncomingMessage(SocketInformation frameObject)
        {
            lock (semaphore)
            {
                if (addPendingInformation.Count == 0 || bytesMessage.Count == 0)
                {
                    return;
                }

                SocketInformation[] findAndRemoveElement = new SocketInformation[addPendingInformation.Count];
                int counter = 0;
                foreach (var element1 in addPendingInformation)
                {
                    if (element1.TargetUser.Equals(frameObject.User) && frameObject.Block == false)
                    {
                        Socket findActiveUser = frameObject.SocketValue;
                        byte[] buffer2 = Encoding.ASCII.GetBytes(element1.User + "*" + element1.Message + "*" + element1.Time);
                        Thread.Sleep(50);

                        findActiveUser.Send(buffer2);
                        findAndRemoveElement[counter] = element1;
                        counter++;
                    }
                }

                for (int i = 0; i < findAndRemoveElement.Length; i++)
                {
                    addPendingInformation.Remove(findAndRemoveElement[i]);
                }

            }
        }

        private void GenerateNewUserConnection()
        {
            Socket handler = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            handler = socketServer.Accept();

            t2 = new Thread(() =>
            {
                int countIfSocketClose = 0;
                while (flag)
                {
                    if (countIfSocketClose > 0)
                    {
                        return;
                    }
                    countIfSocketClose++;
                    MainConversationLogic(handler);
                }
            });
            t2.Start();
        }

        private Socket FindReceiverFromStack(SocketInformation frameObject1, int bytesReadMessage, byte[] buffer)
        {
            lock (semaphore)
            {
                Socket returnSocket = null;
                if (bytesReadMessage == 0)
                {
                    foreach (var element in bytesMessage)
                    {
                        if (element.User.Equals(frameObject1.User))
                        {
                            element.Block = true;
                        }
                    }
                    return null;
                }
                returnSocket = giveSocketToCommunication(frameObject1);

                if (returnSocket != null)
                {
                    returnSocket.Send(buffer);
                }
                else
                {
                    addPendingInformation.Add(frameObject1);
                }
                return null;
            }
        }

        private Socket giveSocketToCommunication(SocketInformation frameObject1)
        {
            Boolean objectExist = false;
            Socket sendToUser = null;

            if (!bytesMessage.IsEmpty)
            {
                foreach (var element in bytesMessage)
                {
                    if (element.User.Equals(frameObject1.User))
                    {
                        objectExist = true;
                        CheckIfUserHasIncomingMessage(frameObject1);
                        break;
                    }
                }
                if (!objectExist)
                {
                    bytesMessage.Enqueue(frameObject1);
                    CheckIfUserHasIncomingMessage(frameObject1);
                }

                foreach (var element in bytesMessage)
                {
                    if (element.User.Equals(frameObject1.TargetUser) && element.Block == false)
                    {
                        sendToUser = element.SocketValue;
                    }
                }
            }
            else
            {
                bytesMessage.Enqueue(frameObject1);
            }

            return sendToUser;
        }

        private void MainConversationLogic(Socket connection)
        {
            try
            {
                SocketInformation socketInfo = null;
                SocketInformation checkValueObject = null;
                SocketInformation setSocketInClass = null;
                int bytesRead = 0;
                int checkObject = 0;
 
                while (true)
                {
                    byte[] buffer1 = new byte[connection.ReceiveBufferSize];
                    bytesRead = connection.Receive(buffer1);

                    if (bytesRead == 0)
                    {
                        FindReceiverFromStack(socketInfo, bytesRead, buffer1);
                        return;
                    }
                    string frame = Encoding.ASCII.GetString(buffer1, 0, bytesRead);
                    string[] words = frame.Split('*');
                    socketInfo = checkIfUserIsBlocked(socketInfo, connection, words);
                    setSocketInClass = socketInfo;
                    if (checkObject >= 1 && ((!checkValueObject.User.Equals(setSocketInClass.User))))
                    {
                        byte[] changeUser = Encoding.ASCII.GetBytes("error");
                        FindReceiverFromStack(checkValueObject, bytesRead, changeUser);
                        return;
                    }
                    else
                    {
                        checkValueObject = setSocketInClass;
                        checkObject++;
                    }

                    FindReceiverFromStack(setSocketInClass, bytesRead, buffer1);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                connection.Close();
            }
        }

        private SocketInformation checkIfUserIsBlocked(SocketInformation socketInfo, Socket connection, string[] words)
        {
            lock (semaphore)
            {
                socketInfo = new SocketInformation();
                socketInfo.User = words[0];
                socketInfo.Message = words[1];
                socketInfo.TargetUser = words[2];
                socketInfo.Time = words[3];
                socketInfo.Block = false;
                socketInfo.SocketValue = connection;

                SocketInformation changeUser = null;
                foreach (var element in bytesMessage)
                {
                    if (element.User.Equals(socketInfo.User) && element.Block == true)
                    {
                        element.SocketValue = socketInfo.SocketValue;
                        element.TargetUser = socketInfo.TargetUser;
                        element.Message = socketInfo.Message;
                        element.Block = false;
                        element.Time = socketInfo.Time;
                        changeUser = element;
                        return changeUser;
                    }
                }
                return socketInfo;
            }
        }

        protected override void OnStop()
        {
            flag = false;
            bytesMessage = new ConcurrentQueue<SocketInformation>();
            addPendingInformation = new List<SocketInformation>();
        }
    }
    public class SocketInformation
    {
        private string targetUser;
        private string user;
        private string message;
        private Boolean isBlocked;
        private Socket socketInfo;
        private string time;

        public string TargetUser
        {
            get { return targetUser; }
            set { targetUser = value; }
        }

        public string User
        {
            get { return user; }
            set { user = value; }
        }

        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        public bool Block
        {
            get { return isBlocked; }
            set { isBlocked = value; }
        }

        public Socket SocketValue
        {
            get { return socketInfo; }
            set { socketInfo = value; }
        }
        public string Time
        {
            get { return time; }
            set { time = value; }
        }

        public SocketInformation()
        {

        }
    }
}
