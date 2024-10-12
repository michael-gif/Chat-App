using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Data.SQLite;

namespace Chat_App_Server
{
    public enum PacketType
    {
        USERNAME,
        CHANNEL_CHANGE,
        CHANNEL_LIST,
        CHANNEL_HISTORY,
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

    internal class Program
    {
        private const string databaseFile = "message_history.db";
        private const string connectionString = "Data Source=message_history.db;Version=3;";

        static List<string> discriminators = new List<string>();
        static Dictionary<string, string> clientUsernames = new Dictionary<string, string>();
        static Dictionary<int, string> channels = new Dictionary<int, string>
        {
            { 0, "Channel 0" },
            { 1, "Channel 1" },
            { 2, "Channel 2" }
        };
        static Dictionary<Socket, int> clientChannels = new Dictionary<Socket, int>();

        static async Task Main(string[] args)
        {
            int port = 11000;
            if (args.Length > 0) port = int.Parse(args[0]);

            InitializeDatabase();

            // Create socket
            IPHostEntry localhost = await Dns.GetHostEntryAsync("localhost");
            IPAddress localIpAddress = localhost.AddressList[0];
            IPEndPoint ipEndPoint = new(localIpAddress, port);
            using Socket listener = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(ipEndPoint);
            listener.Listen(100);
            Console.WriteLine($"{GetTimeStamp()} Listening for connections on [{ipEndPoint.Address}, {ipEndPoint.Port}]...");
            var clients = new ConcurrentBag<Socket>(); // Thread-safe collection of clients

            // Listen for new clients
            while (true)
            {
                var handler = await listener.AcceptAsync();
                clients.Add(handler); // Add the client to the list

                // Handle client communication in a separate task
                _ = Task.Run(() => HandleClient(handler, clients));
            }
        }

        /// <summary>
        /// If the database doesn't already exist, create it
        /// </summary>
        static void InitializeDatabase()
        {
            if (!File.Exists(databaseFile))
            {
                SQLiteConnection.CreateFile(databaseFile);

                // Create the Messages table
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string createTableQuery = @"
                    CREATE TABLE Messages (
                        MessageId INTEGER PRIMARY KEY AUTOINCREMENT,
                        ChannelId TINYINT NOT NULL,
                        Username TEXT NOT NULL,
                        Message TEXT NOT NULL,
                        Timestamp TEXT NOT NULL
                    );";
                    using (var command = new SQLiteCommand(createTableQuery, connection))
                        command.ExecuteNonQuery();
                }
                Console.WriteLine($"{GetTimeStamp()} Database and Messages table created.");
            }
            else
            {
                Console.WriteLine($"{GetTimeStamp()} Found database {databaseFile}");
            }
        }

        /// <summary>
        /// Stores a chat message in the database
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="chatMessage"></param>
        static void StoreMessage(int channelId, ChatMessage chatMessage)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string insertQuery = @"
                INSERT INTO Messages (ChannelId, Username, Message, Timestamp)
                VALUES (@ChannelId, @Username, @Message, @Timestamp);";

                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@ChannelId", channelId);
                    command.Parameters.AddWithValue("@Username", chatMessage.Username);
                    command.Parameters.AddWithValue("@Message", chatMessage.Message);
                    command.Parameters.AddWithValue("@Timestamp", chatMessage.Timestamp);

                    command.ExecuteNonQuery();
                }
            }

            Console.WriteLine($"{GetTimeStamp()} Message from {chatMessage.Username} stored in channel {channelId}.");
        }

        /// <summary>
        /// Gets the most recent 100 messages from the given channel
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        static List<string> GetLast100Messages(int channelId)
        {
            var messages = new List<string>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string selectQuery = @"
                SELECT Username, Message, Timestamp
                FROM Messages
                WHERE ChannelId = @ChannelId
                ORDER BY Timestamp ASC
                LIMIT 100;";

                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@ChannelId", channelId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var chatMessage = new ChatMessage
                            {
                                Username = reader.GetString(0),  // Username
                                Message = reader.GetString(1),   // Message
                                Timestamp = reader.GetString(2)  // Timestamp
                            };
                            // Serialize message into json, then add it to the list
                            messages.Add(JsonSerializer.Serialize(chatMessage));
                        }
                    }
                }
            }

            Console.WriteLine($"{GetTimeStamp()} Retrieved {messages.Count} messages from channel {channelId}.");
            return messages;
        }

        static async Task HandleClient(Socket clientSocket, ConcurrentBag<Socket> clients) {

            IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
            string clientSignature = $"[{clientEndPoint.Address}, {clientEndPoint.Port}]";
            Console.WriteLine($"{GetTimeStamp()} Established connection with {clientSignature}");

            // Read the length of the username first
            var packetLengthBuffer = new byte[4];
            var packetLengthReceived = await clientSocket.ReceiveAsync(packetLengthBuffer, SocketFlags.None);
            if (packetLengthReceived == 0)
            {
                Console.WriteLine($"{GetTimeStamp()} Client {clientSignature} disconnected before sending username.");
                return;
            }

            // Obtain username length
            int packetLength = BitConverter.ToInt32(packetLengthBuffer, 0);
            var packetBuffer = new byte[packetLength];
            var totalBytesReceived = 0;

            // Read the username
            while (totalBytesReceived < packetLength)
            {
                var currentReceived = await clientSocket.ReceiveAsync(new ArraySegment<byte>(packetBuffer, totalBytesReceived, packetLength - totalBytesReceived), SocketFlags.None);
                totalBytesReceived += currentReceived;
            }

            string serializedPacket = Encoding.UTF8.GetString(packetBuffer);
            Packet packet = JsonSerializer.Deserialize<Packet>(serializedPacket);
            string clientUsername = packet.Payload;
            Console.WriteLine($"{GetTimeStamp()} Identified username from {clientSignature}: '{clientUsername}' ({packetLength} bytes)");

            // Create a new discriminator and send it to the client
            string discriminator = CreateNewDiscriminator();
            SendMessageToClient(clientSocket, new Packet(PacketType.DISCRIMINATOR, discriminator));
            Console.WriteLine($"{GetTimeStamp()} Sent new discriminator to {clientSignature}, Username:{clientUsername}, DSCRM:{discriminator}");
            clientUsernames[clientSignature] = $"{clientUsername}#{discriminator}";

            // Send online user list to new user
            SendMessageToClient(clientSocket, new Packet(PacketType.USER_LIST, JsonSerializer.Serialize(clientUsernames.Values)));
            Console.WriteLine($"{GetTimeStamp()} Sent user list to {clientSignature}, Username:{clientUsername}#{discriminator}");

            // Send channel list to new user
            SendMessageToClient(clientSocket, new Packet(PacketType.CHANNEL_LIST, JsonSerializer.Serialize(channels)));
            Console.WriteLine($"{GetTimeStamp()} Sent channel list to {clientSignature}, Username:{clientUsername}#{discriminator}");
            clientChannels[clientSocket] = 0;

            // Update all clients about new user
            BroadcastMessageToAllClients(new Packet(PacketType.USER_CONNECTED, $"{clientUsername}#{discriminator}"), clients, clientSocket);
            Console.WriteLine($"{GetTimeStamp()} Updated all clients about {clientSignature}, Username:{clientUsername}#{discriminator}");

            // Listen for actual messages from the client
            while (true)
            {
                try
                {
                    // Read the 4-byte message length
                    var lengthBuffer = new byte[4];
                    var received = await clientSocket.ReceiveAsync(lengthBuffer, SocketFlags.None);

                    // Client disconnected
                    if (received == 0)
                    {
                        OnClientDisconnect(clients, clientSocket, clientSignature);
                        return;
                    }

                    // Obtain message length and read that many bytes
                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    var messageBuffer = new byte[messageLength];
                    var totalReceived = 0;
                    while (totalReceived < messageLength)
                    {
                        var currentReceived = await clientSocket.ReceiveAsync(new ArraySegment<byte>(messageBuffer, totalReceived, messageLength - totalReceived), SocketFlags.None);
                        totalReceived += currentReceived;
                    }

                    string serializedJson = Encoding.UTF8.GetString(messageBuffer, 0, totalReceived);
                    Packet receivedPacket = JsonSerializer.Deserialize<Packet>(serializedJson);
                    switch (receivedPacket.Type)
                    {
                        case PacketType.CHANNEL_CHANGE:
                            int newClientChannel = receivedPacket.Channel;
                            int oldChannel = clientChannels[clientSocket];
                            clientChannels[clientSocket] = newClientChannel;
                            Console.WriteLine($"{GetTimeStamp()} Channel change: {clientSignature}, Username:{clientUsernames[clientSignature]} from {oldChannel} to {newClientChannel}");
                            
                            // Send channel history
                            List<string> channelHistory = GetLast100Messages(newClientChannel);
                            SendMessageToClient(clientSocket, new Packet(PacketType.CHANNEL_HISTORY, JsonSerializer.Serialize(channelHistory)));
                            Console.WriteLine($"{GetTimeStamp()} Sent channel {newClientChannel} history to {clientSignature}, Username:{clientUsername}#{discriminator}");
                            continue;
                        case PacketType.CHAT_MESSAGE:
                            Console.WriteLine($"{GetTimeStamp()} Received message from {clientSignature}, Username:{clientUsernames[clientSignature]} ({messageLength} bytes, Channel:{receivedPacket.Channel})");
                            StoreMessage(receivedPacket.Channel, JsonSerializer.Deserialize<ChatMessage>(receivedPacket.Payload));
                            BroadcastMessageToAllClients(receivedPacket, clients, null);
                            Console.WriteLine($"{GetTimeStamp()} Sent message from {clientSignature}, Username:{clientUsernames[clientSignature]} ({messageLength} bytes, Channel:{receivedPacket.Channel}) to all clients");
                            break;
                    }
                }
                catch (SocketException)
                {
                    // Handle any socket errors (like disconnects)
                    OnClientDisconnect(clients, clientSocket, clientSignature);
                    break;
                }
            }
        }

        /// <summary>
        /// Update all connected clients about the disconnection, then cleanup dead client data
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="clientSocket"></param>
        /// <param name="clientSignature"></param>
        static void OnClientDisconnect(ConcurrentBag<Socket> clients, Socket clientSocket, string clientSignature)
        {
            // Update connected clients about disconnection
            string clientUsername = clientUsernames[clientSignature];
            Console.WriteLine($"{GetTimeStamp()} Client disconnected: {clientSignature}, Username:{clientUsername}");
            BroadcastMessageToAllClients(new Packet(PacketType.USER_DISCONNECTED, clientUsername), clients, clientSocket);
            Console.WriteLine($"{GetTimeStamp()} Updated all clients about disconnection by: {clientSignature}, Username:{clientUsername}");

            // Cleanup dead client data
            discriminators.Remove(clientUsername.Split("#")[1]);
            clientUsernames.Remove(clientSignature);
            clientChannels.Remove(clientSocket);
            clients.TryTake(out clientSocket); // Remove from client list
        }

        /// <summary>
        /// Take the message and broadcast it to all the clients. If 'excludedSender' is not null, every client except 'excludedSender' will be sent the message.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="clients"></param>
        /// <param name="excludedSender"></param>
        static void BroadcastMessageToAllClients(Packet packet, ConcurrentBag<Socket> clients, Socket excludedSender)
        {
            foreach (var client in clients)
            {
                // If sender is excluded
                if (excludedSender != null && client == excludedSender) continue;
                // If client is not on specified channel
                if (packet.Channel != -1 && !(clientChannels[client] == packet.Channel)) continue;
                // Send message
                SendMessageToClient(client, packet);
            }
        }

        /// <summary>
        /// Send message to individual client, prepending it with the length of the message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="packet"></param>
        static void SendMessageToClient(Socket client, Packet packet)
        {
            try
            {
                string packetJson = JsonSerializer.Serialize(packet);
                var packetBytes = Encoding.UTF8.GetBytes(packetJson);
                var packetLengthBytes = BitConverter.GetBytes(packetBytes.Length);
                List<byte> finalBytes = new();
                finalBytes.AddRange(packetLengthBytes); // Prepend the message with the length bytes
                finalBytes.AddRange(packetBytes);
                // Send the message
                client.Send(finalBytes.ToArray());
            }
            catch (SocketException)
            {
                // Handle if a client is disconnected or unreachable
                Console.WriteLine($"Failed to send message to client {client.RemoteEndPoint}");
            }
        }

        /// <summary>
        /// Gets today's timestamp in the format: [yyyy/MM/dd HH:mm:ss]
        /// </summary>
        /// <returns></returns>
        static string GetTimeStamp()
        {
            return DateTime.Now.ToString("[yyyy/MM/dd HH:mm:ss]");
        }

        /// <summary>
        /// Generate a random unique 4 digit string in the format: ####
        /// </summary>
        /// <returns></returns>
        static string CreateNewDiscriminator()
        {
            while (true)
            {
                Random random = new Random();
                int randomNumber = random.Next(1000, 10000); // Random number between 1000 and 9999
                string discriminator = randomNumber.ToString();
                if (discriminators.Contains(discriminator)) continue;
                return discriminator;
            }
        }
    }
}