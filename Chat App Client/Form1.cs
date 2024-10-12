using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chat_App_Client
{
    public partial class Form1 : Form
    {
        Socket? client;
        readonly string username = "";
        string usernameDiscriminator = "";
        Thread receiveMessageThread;
        CancellationTokenSource cancellationTokenSource;
        int selectedChannel = -1;
        public Form1(string username)
        {
            this.username = username;
            cancellationTokenSource = new CancellationTokenSource();
            InitializeComponent();
            Text = "Chat App - Disconnected: " + username;
            messageHistoryGridView.MouseWheel += new MouseEventHandler(messageHistoryGridView_MouseWheel);
        }

        public enum PacketType
        {
            USERNAME,
            CHANNEL_CHANGE,
            CHANNEL_LIST,
            DISCRIMINATOR,
            USER_CONNECTED,
            USER_DISCONNECTED,
            USER_LIST,
            CHAT_MESSAGE
        }

        public class Packet
        {
            public PacketType Type { get; set; }
            public int Channel { get; set; } = -1;
            public string Payload { get; set; }

            [JsonConstructor]
            public Packet(PacketType type, int channel, string payload)
            {
                Type = type;
                Channel = channel;
                Payload = payload;
            }

            public Packet(PacketType type, string payload)
            {
                Type = type;
                Payload = payload;
            }

            public Packet(PacketType type, int channel)
            {
                Type = type;
                Channel = channel;
            }
        }

        public class ChatMessage
        {
            public string Username { get; set; }
            public string Message { get; set; }
            public string Timestamp { get; set; }
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
                SendChatMessage(textBox1.Text);
            }
        }

        /// <summary>
        /// Change the dimensions of the textbox and listview according to the number of lines in the textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            int lines = textBox1.Lines.Length;
            int diff = (lines * 16) + 4 - textBox1.Height;
            if (diff != 0)
            {
                textBox1.Location = new Point(textBox1.Left, textBox1.Bottom - Math.Clamp((lines * 16) + 4, 20, 84));
                textBox1.Height = Math.Clamp((lines * 16) + 4, 20, 84);

                // Window height - listview location Y - gap at bottom of window - text box height - gap between listview and textbox
                messageHistoryGridView.Height = Height - messageHistoryGridView.Location.Y - 51 - textBox1.Height - 6;
            }
        }

        /// <summary>
        /// Sends message on button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendMessageButton_Click(object sender, EventArgs e)
        {
            SendChatMessage(textBox1.Text);
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
        /// Connect to server button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void connectToServerButton_Click(object sender, EventArgs e)
        {
            if (connectToServerButton.Text == "Connect to server")
            {
                ConnectToServerForm connectToServerForm;
                bool cancelled = false;
                while (true)
                {
                    connectToServerForm = new ConnectToServerForm();
                    DialogResult result = connectToServerForm.ShowDialog();
                    if (result == DialogResult.Cancel)
                    {
                        cancelled = true;
                        break;
                    }

                    // User didn't provide an address
                    if (string.IsNullOrEmpty(connectToServerForm.Address))
                    {
                        MessageBox.Show("You did not provide an address", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }
                    break;
                }
                if (cancelled) return;
                await ConnectToServer(connectToServerForm.Address, connectToServerForm.Port);
            }
            else
            {
                DisconnectFromServer();
            }
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

        /// <summary>
        /// Switch channel to selectedChannel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void channelsListView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (!e.IsSelected) return;
            selectedChannel = (int)e.Item.Tag;
            messageHistoryGridView.Rows.Clear();
            SendPacketToServer(new Packet(PacketType.CHANNEL_CHANGE, selectedChannel));
            Console.WriteLine("Switched to channel: " + selectedChannel);
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
        /// Gets today's timestamp in the format: [yyyy/MM/dd HH:mm:ss]
        /// </summary>
        /// <returns></returns>
        private string GetTimeStamp()
        {
            return DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        }

        /// <summary>
        /// Sends message to server
        /// </summary>
        /// <param name="message"></param>
        private async void SendChatMessage(string message)
        {
            if (client == null) return;

            // Create message
            if (string.IsNullOrEmpty(message)) return;
            var chatMessage = new ChatMessage
            {
                Username = username + usernameDiscriminator,
                Message = message,
                Timestamp = GetTimeStamp()
            };

            SendPacketToServer(new Packet(PacketType.CHAT_MESSAGE, selectedChannel, JsonSerializer.Serialize(chatMessage)));
            textBox1.Clear();
        }

        private async void SendPacketToServer(Packet packet)
        {
            // Create packet
            string packetJson = JsonSerializer.Serialize(packet);
            byte[] packetBytes = Encoding.UTF8.GetBytes(packetJson);
            int packetLength = packetBytes.Length;
            byte[] lengthBytes = BitConverter.GetBytes(packetLength);

            // Prepend packet length to packet
            List<byte> finalBytes = new();
            finalBytes.AddRange(lengthBytes);
            finalBytes.AddRange(packetBytes);

            // Send it
            await client.SendAsync(finalBytes.ToArray(), SocketFlags.None);
        }

        /// <summary>
        /// Adds message to message window on the client
        /// </summary>
        /// <param name="username"></param>
        /// <param name="message"></param>
        private void AddMessageToWindow(string username, string message, string timestamp)
        {
            messageHistoryGridView.Rows.Add(new string[] { username + "\n" + timestamp, message });
            // Scroll history to the bottom
            messageHistoryGridView.FirstDisplayedScrollingRowIndex = messageHistoryGridView.RowCount - 1;
        }

        /// <summary>
        /// Adds username to listview of online users
        /// </summary>
        /// <param name="username"></param>
        private void AddOnlineUser(string username)
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
        /// Adds a channel to the channels treeview
        /// </summary>
        /// <param name="channelName"></param>
        private void AddChannel(string channelName)
        {
            ListViewItem channel = new ListViewItem(channelName);
            channel.Tag = channelsListView.Items.Count;
            channelsListView.Items.Add(channel);
        }

        /// <summary>
        /// Selects a channel, highlighting it.
        /// </summary>
        /// <param name="channelIndex"></param>
        private void SelectChannel(int channelIndex)
        {
            foreach (ListViewItem item in channelsListView.Items) item.Selected = false;
            channelsListView.Items[channelIndex].Selected = true;
            channelsListView.Select();
        }

        /// <summary>
        /// Connect to server
        /// </summary>
        /// <returns></returns>
        private async Task ConnectToServer(string hostname, int port)
        {
            // Create new socket
            IPHostEntry hostEntry = await Dns.GetHostEntryAsync(hostname);
            IPAddress ipAddress = hostEntry.AddressList[0];
            IPEndPoint ipEndPoint = new(ipAddress, port);
            client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine($"Attempting to connect to [{ipEndPoint.Address}, {ipEndPoint.Port}]...");

            // Disable connect button
            connectToServerButton.Enabled = false;
            connectToServerButton.Text = "Connecting...";

            // Attempt to connect to the server
            try
            {
                await client.ConnectAsync(ipEndPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect to server");
                Console.WriteLine(ex.ToString());
                connectToServerButton.Text = "Connect to server";
                connectToServerButton.Enabled = true;
                client.Close();
                client = null;
                MessageBox.Show("Failed to connect to server", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Console.WriteLine($"Connected to [{ipEndPoint.Address}, {ipEndPoint.Port}]");

            // Send username to server
            SendPacketToServer(new Packet(PacketType.USERNAME, username));

            // Renable connect button
            connectToServerButton.Text = "Disconnect";
            connectToServerButton.Enabled = true;

            // Start thread to receive messages
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
            channelsListView.Items.Clear();
            cancellationTokenSource = new CancellationTokenSource();
            usernameDiscriminator = "";
            Text = "Chat App - Disconnected: " + username;
            connectToServerButton.Text = "Connect to server";
            connectToServerButton.Enabled = true;
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
                    // If cancellation requested, throw an exception
                    cancellationToken.ThrowIfCancellationRequested();

                    // Read the 4-byte message length
                    var lengthBuffer = new byte[4];
                    var received = await client.ReceiveAsync(lengthBuffer, SocketFlags.None);

                    // Obtain message length and read that many bytes
                    int packetLength = BitConverter.ToInt32(lengthBuffer, 0);
                    var packetBuffer = new byte[packetLength];
                    var totalReceived = 0;
                    while (totalReceived < packetLength)
                    {
                        var currentReceived = await client.ReceiveAsync(new ArraySegment<byte>(packetBuffer, totalReceived, packetLength - totalReceived), SocketFlags.None);
                        totalReceived += currentReceived;
                    }

                    // Decode the message
                    var serializedPacket = Encoding.UTF8.GetString(packetBuffer, 0, totalReceived);
                    Packet packet = JsonSerializer.Deserialize<Packet>(serializedPacket);
                    switch (packet.Type) {
                        case PacketType.CHANNEL_LIST: // Channel list
                            Dictionary<int, string> channelList = JsonSerializer.Deserialize<Dictionary<int, string>>(packet.Payload);
                            Console.WriteLine($"Channels received: {channelList.Count}");
                            foreach (string channelName in channelList.Values) AddChannel(channelName);
                            SelectChannel(0);
                            break;
                        case PacketType.DISCRIMINATOR:
                            string discriminator = packet.Payload;
                            Console.WriteLine($"Discriminator recieved: {discriminator}");
                            usernameDiscriminator = "#" + discriminator;
                            Text = $"Chat App - Connected: {username}{usernameDiscriminator}";
                            break;
                        case PacketType.USER_CONNECTED:
                            string newUsername = packet.Payload;
                            Console.WriteLine($"New user online, username received: {newUsername}");
                            AddOnlineUser(newUsername);
                            break;
                        case PacketType.USER_DISCONNECTED:
                            string disconnectedUsername = packet.Payload;
                            Console.WriteLine($"User disconnected: {disconnectedUsername}");
                            RemoveOnlineUser(disconnectedUsername);
                            break;
                        case PacketType.USER_LIST:
                            List<string> onlineUserList = JsonSerializer.Deserialize<List<string>>(packet.Payload);
                            Console.WriteLine($"Online user list received, {onlineUserList.Count} users");
                            foreach (string onlineUsername in onlineUserList) AddOnlineUser(onlineUsername);
                            break;
                        case PacketType.CHAT_MESSAGE:
                            ChatMessage deserializedJson = JsonSerializer.Deserialize<ChatMessage>(packet.Payload);
                            AddMessageToWindow(deserializedJson.Username, deserializedJson.Message, deserializedJson.Timestamp);
                            break;
                        default:
                            Console.WriteLine($"Recieved unknown packet type: {packet.Type.ToString()}");
                            break;
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
    }
}