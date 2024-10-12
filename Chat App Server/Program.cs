using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Chat_App_Server
{
    internal class Program
    {
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

        static async Task HandleClient(Socket clientSocket, ConcurrentBag<Socket> clients) {

            IPEndPoint clientEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
            string clientSignature = $"[{clientEndPoint.Address}, {clientEndPoint.Port}]";
            Console.WriteLine($"{GetTimeStamp()} Established connection with {clientSignature}");

            // Read the length of the username first
            var usernameLengthBuffer = new byte[4];
            var usernameLengthReceived = await clientSocket.ReceiveAsync(usernameLengthBuffer, SocketFlags.None);
            if (usernameLengthReceived == 0)
            {
                Console.WriteLine($"{GetTimeStamp()} Client {clientSignature} disconnected before sending username.");
                return;
            }

            // Obtain username length
            int usernameLength = BitConverter.ToInt32(usernameLengthBuffer, 0);
            var usernameBuffer = new byte[usernameLength];
            var totalUsernameReceived = 0;

            // Read the username
            while (totalUsernameReceived < usernameLength)
            {
                var currentReceived = await clientSocket.ReceiveAsync(new ArraySegment<byte>(usernameBuffer, totalUsernameReceived, usernameLength - totalUsernameReceived), SocketFlags.None);
                totalUsernameReceived += currentReceived;
            }
            string username = Encoding.UTF8.GetString(usernameBuffer);
            Console.WriteLine($"{GetTimeStamp()} Identified username from {clientSignature}: '{username}' ({usernameLength} bytes)");

            // Create a new discriminator and send it to the client
            string discriminator = CreateNewDiscriminator();
            SendMessageToClient(clientSocket, Encoding.UTF8.GetBytes("<|DSCRM|>" + discriminator));
            Console.WriteLine($"{GetTimeStamp()} Sent new discriminator to {clientSignature}, Username:{username}, DSCRM:{discriminator}");
            clientUsernames[clientSignature] = $"{username}#{discriminator}";

            // Send channel list to new user
            string jsonChannelList = JsonSerializer.Serialize(channels);
            SendMessageToClient(clientSocket, Encoding.UTF8.GetBytes("<|CHLST|>" + jsonChannelList));
            Console.WriteLine($"{GetTimeStamp()} Sent channel list to {clientSignature}, Username:{username}#{discriminator}");
            clientChannels[clientSocket] = 0;

            // Send online user list to new user
            string jsonUserList = JsonSerializer.Serialize(clientUsernames.Values);
            SendMessageToClient(clientSocket, Encoding.UTF8.GetBytes("<|USRLST|>" + jsonUserList));
            Console.WriteLine($"{GetTimeStamp()} Sent user list to {clientSignature}, Username:{username}#{discriminator}");

            // Update all clients about new user
            BroadcastMessageToAllClients(Encoding.UTF8.GetBytes($"<|NEWUSR|>{username}#{discriminator}"), -1, clients, clientSocket);
            Console.WriteLine($"{GetTimeStamp()} Updated all clients about {clientSignature}, Username:{username}#{discriminator}");

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

                    var clientMessage = Encoding.UTF8.GetString(messageBuffer, 0, totalReceived);
                    if (clientMessage.StartsWith("<|CHCHGE|>"))
                    {
                        int newClientChannel = int.Parse(clientMessage.Replace("<|CHCHGE|>", ""));
                        int oldChannel = clientChannels[clientSocket];
                        clientChannels[clientSocket] = newClientChannel;
                        Console.WriteLine($"{GetTimeStamp()} Channel change: {clientSignature}, Username:{clientUsernames[clientSignature]} from {oldChannel} to {newClientChannel}");
                        continue;
                    }

                    // Remove channel from message
                    int clientChannel = BitConverter.ToInt32(messageBuffer, 0);
                    var messageWithoutChannel = new byte[messageLength - 4];
                    Array.Copy(messageBuffer, 4, messageWithoutChannel, 0, messageLength - 4);

                    Console.WriteLine($"{GetTimeStamp()} Received message from {clientSignature}, Username:{clientUsernames[clientSignature]} ({messageLength} bytes, Channel:{clientChannel})");
                    BroadcastMessageToAllClients(messageWithoutChannel, clientChannel, clients, null);
                    Console.WriteLine($"{GetTimeStamp()} Sent message from {clientSignature}, Username:{clientUsernames[clientSignature]} ({messageLength} bytes, Channel:{clientChannel}) to all clients");
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
            BroadcastMessageToAllClients(Encoding.UTF8.GetBytes($"<|USRDC|>{clientUsername}"), -1, clients, clientSocket);
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
        /// <param name="messageBuffer">The message</param>
        /// <param name="clients"></param>
        /// <param name="excludedSender">Socket to exclude from being sent a message</param>
        static void BroadcastMessageToAllClients(byte[] messageBuffer, int channel, ConcurrentBag<Socket> clients, Socket excludedSender)
        {
            foreach (var client in clients)
            {
                // if sender is excluded
                if (excludedSender != null && client == excludedSender) continue;
                // if client is not on specified channel
                if (channel != -1 && !(clientChannels[client] == channel)) continue;
                // send message
                SendMessageToClient(client, messageBuffer);
            }
        }

        /// <summary>
        /// Send message to individual client, prepending it with the length of the message
        /// </summary>
        /// <param name="client"></param>
        /// <param name="messageBuffer"></param>
        static void SendMessageToClient(Socket client, byte[] messageBuffer)
        {
            try
            {
                var messageBufferLengthBytes = BitConverter.GetBytes(messageBuffer.Length);
                // Prepend the message with the length bytes
                byte[] finalMessage = new byte[messageBufferLengthBytes.Length + messageBuffer.Length];
                Array.Copy(messageBufferLengthBytes, 0, finalMessage, 0, messageBufferLengthBytes.Length);
                Array.Copy(messageBuffer, 0, finalMessage, messageBufferLengthBytes.Length, messageBuffer.Length);
                // Send the message
                client.Send(finalMessage);
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