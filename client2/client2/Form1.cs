using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client2
{
    public partial class Form1 : Form
    {
        Socket socketClient;
        IPAddress ipAddress;
        IPEndPoint localEndPoint;
        Thread t1;
        DateTime oDate;
        DateTime oDate1;
        TimeSpan value;

        string frame = "";
        string messageBack = "";
        string dataReceived = "";
        int bytesRead1 = 0;
        byte[] bytesToSend;
        byte[] buffer;
        string[] words;
        string substringDate = "";
        int countConnection = 0;
        string[] result;
        int wynik = 0;
        public Form1()
        {
            InitializeComponent();
            //projekt testowy
            ipAddress = System.Net.IPAddress.Parse("127.0.0.1");
            localEndPoint = new IPEndPoint(ipAddress, 8000);
            socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!socketClient.Connected)
            {
                if (countConnection >= 1)
                {
                    socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
                socketClient.Connect(localEndPoint);
                countConnection++;
            }
            else
            {
                MessageBox.Show("You are already connected");
                return;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (socketClient.Connected)
            {
                socketClient.Disconnect(false);
                socketClient.Close();
            }
            else
            {
                MessageBox.Show("Please earlier connect with service");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            for(int i = 0;i< 500;i++)
            {
                Thread.Sleep(200);
                string infoTextBoxMessage = textBox1.Text;
                string user = textBox2.Text;
                string targetUser = textBox3.Text;
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff",
                                                CultureInfo.InvariantCulture);

                if (String.IsNullOrEmpty(user) || String.IsNullOrEmpty(infoTextBoxMessage))
                {
                    MessageBox.Show("Please complete the message or username");
                    return;
                }
                if (!socketClient.Connected)
                {
                    MessageBox.Show("Please start connection");
                    return;
                }

                frame = user + "*" + infoTextBoxMessage + "*" + targetUser + "*" + timestamp;

                bytesToSend = ASCIIEncoding.ASCII.GetBytes(frame);

                socketClient.Send(bytesToSend);

                listBox1.TopIndex = listBox1.Items.Count - 1;

                /*  textBox1.Clear();*/
                timestamp = "";

            }


        }

        private void button2_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            t1 = new Thread(() =>
            {
                while (true)
                {
                    ReceiveInformation();
                }
            });
            t1.Start();
        }

        private void ReceiveInformation()
        {
            try
            {
                if (socketClient.Connected)
                {
                    buffer = new byte[socketClient.ReceiveBufferSize];
                    bytesRead1 = socketClient.Receive(buffer);

                    if (bytesRead1 == 0)
                    {
                        socketClient.Close();
                        return;
                    }
                    dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead1);

                    if (dataReceived.Equals("error"))
                    {
                        MessageBox.Show("Please connect again before changing user. This message will not be sent");
                        return;
                    }

                    words = dataReceived.Split('*');
                    messageBack = words[0] + ":" + words[1];
                    if (words[2].Any(c => char.IsDigit(c)))
                    {
                        substringDate = words[2];
                    }
                    else
                    {
                        substringDate = words[3];
                    }
                    wynik++;
                    string time = substringDate.Substring(11);
                    string[] table = time.Split(new Char[] { ':', '.' });
                    int hours = Int32.Parse(table[0]);
                    int minutes = Int32.Parse(table[1]);
                    int seconds = Int32.Parse(table[2]);
                    int mili = Int32.Parse(table[3]);
                    long resultInMili = (hours * 60 * 60 * 1000) + (minutes * 60 * 1000) + (seconds * 100000) + mili;

                    string timestamp1 = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff",
                                            CultureInfo.InvariantCulture);

                    string currTime = timestamp1.Substring(11);
                    string[] table1 = currTime.Split(new Char[] { ':', '.' });
                    int currHours = Int32.Parse(table1[0]);
                    int currMinutes = Int32.Parse(table1[1]);
                    int currSeconds = Int32.Parse(table1[2]);
                    int currMili = Int32.Parse(table1[3]);
                    long currResultInMili = (currHours * 60 * 60 * 1000) + (currMinutes * 60 * 1000) + (currSeconds * 100000) + currMili;

                    long finalResultTime = currResultInMili - resultInMili;

                    this.Invoke((MethodInvoker)(() => listBox1.Items.Add(messageBack)));
                    this.Invoke((MethodInvoker)(() => listBox2.Items.Add(finalResultTime)));
                    this.Invoke((MethodInvoker)(() => listBox1.TopIndex = listBox1.Items.Count - 1));
                    this.Invoke((MethodInvoker)(() => listBox1.Items.Add(wynik)));

                    dataReceived = "";
                    messageBack = "";
                    substringDate = "";
                    timestamp1 = "";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                socketClient.Close();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            
            for(int i = 0; i< listBox2.Items.Count;i++)
            {
                result[i] = listBox2.Items[i].ToString();
            }
            long l1;
            l1 = long.Parse(result[50]);
            long l2;
            l2 = long.Parse(result[51]);
            long median = (l1 + l2) / 2;
            textBox4.Text = median.ToString();
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
