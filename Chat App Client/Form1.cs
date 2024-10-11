using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace Chat_App_Client
{
    public partial class Form1 : Form
    {
        bool firstTreeExpansion = false;
        Socket? client;
        string username = "";
        string usernameDiscriminator = "";
        Thread receiveMessageThread;
        CancellationTokenSource cancellationTokenSource;
        public Form1(string username)
        {
            this.username = username;
            cancellationTokenSource = new CancellationTokenSource();
            InitializeComponent();
            channelTreeView.ExpandAll();
            Text = "Chat App - Disconnected: " + username;
            messageHistoryGridView.MouseWheel += new MouseEventHandler(messageHistoryGridView_MouseWheel);
        }

        /// <summary>
        /// Prevent the root node Channels from being selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void channelTreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Parent == null)
            {
                channelTreeView.SelectedNode = null;
                e.Node.BackColor = Color.White;
                e.Node.ForeColor = Color.Black;
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Prevent the channel treeview from being expanded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void channelTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (firstTreeExpansion) e.Cancel = true;
            else firstTreeExpansion = true;
        }

        /// <summary>
        /// Prevent the channel treeview from being collapsed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void channelTreeView_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = true;
        }

        /// <summary>
        /// Send message on enter, add new line on shift + enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (e.Shift) return;
                e.SuppressKeyPress = true;
                SendMessage(textBox1.Text);
            }
        }

        /// <summary>
        /// Change the dimensions of the textbox and listview according to the number of lines in the textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateTextBoxSize(object sender, EventArgs args)
        {
            int lines = textBox1.Lines.Length;
            int diff = (lines * 16) + 4 - textBox1.Height;
            if (diff != 0)
            {
                textBox1.Location = new Point(textBox1.Left, textBox1.Bottom - Math.Clamp((lines * 16) + 4, 20, 84));
                textBox1.Height = Math.Clamp((lines * 16) + 4, 20, 84);

                // window height - listview location Y - gap at bottom of window - text box height - gap between listview and textbox
                messageHistoryGridView.Height = Height - messageHistoryGridView.Location.Y - 51 - textBox1.Height - 6;
            }
        }

        /// <summary>
        /// Prevent user from selecting a row
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void messageHistoryGridView_SelectionChanged(object sender, EventArgs e)
        {
            messageHistoryGridView.ClearSelection();
        }

        /// <summary>
        /// When the mouse moves over a row, "highlight" it with light gray
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void messageHistoryGridView_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            messageHistoryGridView.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.LightGray;
        }

        /// <summary>
        /// When the mouse moves off a row, unhighlight it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void messageHistoryGridView_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            messageHistoryGridView.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
        }

        /// <summary>
        /// Sends message on button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendMessageButton_Click(object sender, EventArgs e)
        {
            SendMessage(textBox1.Text);
        }

        /// <summary>
        /// Sends message to server
        /// </summary>
        /// <param name="message"></param>
        private async void SendMessage(string message)
        {
            if (client == null) return;

            // Create message bytes
            if (string.IsNullOrEmpty(message)) return;
            var chatMessage = new ChatMessage { Username = username + usernameDiscriminator, Message = message };
            string serializedJson = JsonSerializer.Serialize(chatMessage);
            byte[] messageBytes = Encoding.UTF8.GetBytes(serializedJson);

            // Calculate the length of the message and convert it to 4 bytes (Int32)
            int messageLength = messageBytes.Length;
            byte[] lengthBytes = BitConverter.GetBytes(messageLength);

            // Prepend the message with the length bytes
            byte[] finalMessage = new byte[lengthBytes.Length + messageBytes.Length];
            Array.Copy(lengthBytes, 0, finalMessage, 0, lengthBytes.Length);
            Array.Copy(messageBytes, 0, finalMessage, lengthBytes.Length, messageBytes.Length);

            // Send it
            await client.SendAsync(finalMessage, SocketFlags.None);
            textBox1.Clear();
        }

        /// <summary>
        /// Adds message to message window on the client
        /// </summary>
        /// <param name="username"></param>
        /// <param name="message"></param>
        private void AddMessageToWindow(string username, string message)
        {
            messageHistoryGridView.Rows.Add(new string[] { username, message });
            // Scroll history to the bottom
            messageHistoryGridView.FirstDisplayedScrollingRowIndex = messageHistoryGridView.RowCount - 1;
        }

        /// <summary>
        /// Adds username to listview of online users
        /// </summary>
        /// <param name="username"></param>
        private void NewOnlineUser(string username)
        {
            onlineUsersListView.Items.Add(username);
        }

        /// <summary>
        /// Removes username from listview of online users
        /// </summary>
        /// <param name="username"></param>
        private void RemoveOnlineUser(string username)
        {
            foreach (ListViewItem user in onlineUsersListView.Items)
                if (user.Text == username)
                    onlineUsersListView.Items.Remove(user);
        }

        /// <summary>
        /// Connect to server
        /// </summary>
        /// <returns></returns>
        private async Task InitSocket()
        {
            IPHostEntry localhost = await Dns.GetHostEntryAsync("localhost");
            IPAddress localIpAddress = localhost.AddressList[0];
            IPEndPoint ipEndPoint = new(localIpAddress, 11_000);
            client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            Console.WriteLine($"Attempting to connect to [{ipEndPoint.Address}, {ipEndPoint.Port}]...");
            connectToServerButton.Enabled = false;
            connectToServerButton.Text = "Connecting...";
            await client.ConnectAsync(ipEndPoint);
            Console.WriteLine($"Connected to [{ipEndPoint.Address}, {ipEndPoint.Port}]");

            // send username to server
            var usernameBytes = Encoding.UTF8.GetBytes(username);
            var usernameLengthBytes = BitConverter.GetBytes(usernameBytes.Length);
            // Prepend the message with the length bytes
            byte[] finalMessage = new byte[usernameLengthBytes.Length + usernameBytes.Length];
            Array.Copy(usernameLengthBytes, 0, finalMessage, 0, usernameLengthBytes.Length);
            Array.Copy(usernameBytes, 0, finalMessage, usernameLengthBytes.Length, usernameBytes.Length);
            await client.SendAsync(finalMessage, SocketFlags.None); // Send username

            connectToServerButton.Text = "Disconnect";
            connectToServerButton.Enabled = true;

            receiveMessageThread = new(() => receiveMessageThreadFunction(cancellationTokenSource.Token));
            receiveMessageThread.Start();
        }

        /// <summary>
        /// Close the socket and clean things up
        /// </summary>
        private void DisconnectFromServer()
        {
            connectToServerButton.Enabled = false;
            cancellationTokenSource.Cancel();
            receiveMessageThread.Join();
            try
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                client = null;
            }
            catch
            {
                ; // fuck me. there is no good way to check if the socket is closed, so just let it error if closed twice
            }
            onlineUsersListView.Items.Clear();
            messageHistoryGridView.Rows.Clear();
            cancellationTokenSource = new CancellationTokenSource();
            usernameDiscriminator = "";
            Text = "Chat App - Disconnected: " + username;
            connectToServerButton.Text = "Connect to server";
            connectToServerButton.Enabled = true;
        }

        /// <summary>
        /// Connect to server button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void connectToServerButton_Click(object sender, EventArgs e)
        {
            if (connectToServerButton.Text == "Connect to server")
            {
                await InitSocket();
            }
            else
            {
                DisconnectFromServer();
            }
        }

        /// <summary>
        /// Listen for message from the server. This runs on a separate thread
        /// </summary>
        private async void receiveMessageThreadFunction(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    // if cancellation requested, throw an exception
                    cancellationToken.ThrowIfCancellationRequested();

                    // Read the 4-byte message length
                    var lengthBuffer = new byte[4];
                    var received = await client.ReceiveAsync(lengthBuffer, SocketFlags.None);

                    // Obtain message length and read that many bytes
                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    var messageBuffer = new byte[messageLength];
                    var totalReceived = 0;
                    while (totalReceived < messageLength)
                    {
                        var currentReceived = await client.ReceiveAsync(new ArraySegment<byte>(messageBuffer, totalReceived, messageLength - totalReceived), SocketFlags.None);
                        totalReceived += currentReceived;
                    }

                    // Decode the message
                    var response = Encoding.UTF8.GetString(messageBuffer, 0, totalReceived);
                    if (response.StartsWith("<|DSCRM|>"))
                    {
                        string discriminator = response.Replace("<|DSCRM|>", "");
                        Console.WriteLine($"Discriminator recieved: {discriminator}");
                        usernameDiscriminator = "#" + discriminator;
                        Text = $"Chat App - Connected: {username}{usernameDiscriminator}";
                    }
                    else if (response.StartsWith("<|NEWUSR|>"))
                    {
                        string newUsername = response.Replace("<|NEWUSR|>", "");
                        Console.WriteLine($"New user online, username received: {newUsername}");
                        NewOnlineUser(newUsername);
                    }
                    else if (response.StartsWith("<|USRDC|>"))
                    {
                        string disconnectedUsername = response.Replace("<|USRDC|>", "");
                        Console.WriteLine($"User disconnected: {disconnectedUsername}");
                        RemoveOnlineUser(disconnectedUsername);
                    }
                    else if (response.StartsWith("<|USRLST|>"))
                    {
                        string jsonList = response.Replace("<|USRLST|>", "");
                        List<string> onlineUserList = JsonSerializer.Deserialize<List<string>>(jsonList);
                        Console.WriteLine($"Online user list received, {onlineUserList.Count} users");
                        foreach (string onlineUsername in onlineUserList)
                        {
                            NewOnlineUser(onlineUsername);
                        }
                    }
                    else
                    {
                        ChatMessage deserializedJson = JsonSerializer.Deserialize<ChatMessage>(response);
                        AddMessageToWindow(deserializedJson.Username, deserializedJson.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: Disconnected from server");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                DisconnectFromServer();
            }
        }

        /// <summary>
        /// Disconnect from the server on form close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (client == null) return;
            DisconnectFromServer();
        }

        /// <summary>
        /// Make the message history scrollable with the mouse wheel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void messageHistoryGridView_MouseWheel(object sender, MouseEventArgs e)
        {
            int currentIndex = messageHistoryGridView.FirstDisplayedScrollingRowIndex;
            int scrollLines = SystemInformation.MouseWheelScrollLines;

            if (e.Delta > 0)
            {
                messageHistoryGridView.FirstDisplayedScrollingRowIndex = Math.Max(0, currentIndex - scrollLines);
            }
            else if (e.Delta < 0)
            {
                messageHistoryGridView.FirstDisplayedScrollingRowIndex = currentIndex + scrollLines;
            }
        }

        public class ChatMessage
        {
            public string Username { get; set; }
            public string Message { get; set; }
        }
    }
}