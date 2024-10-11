using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Chat_App_Server
{
    internal class Program
    {
        static List<string> discriminators = new List<string>();

        static async Task Main(string[] args)
        {
            IPHostEntry localhost = await Dns.GetHostEntryAsync("localhost");
            IPAddress localIpAddress = localhost.AddressList[0];
            IPEndPoint ipEndPoint = new(localIpAddress, 11_000);
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
            Console.WriteLine($"{GetTimeStamp()} Identified username from {clientSignature}: {username} ({usernameLength} bytes)");

            // Create a new discriminator and send it to the client
            string discriminator = CreateNewDiscriminator();
            SendMessageToClient(clientSocket, Encoding.UTF8.GetBytes("<|DSCRM|>" + discriminator));
            Console.WriteLine($"{GetTimeStamp()} Sent new discriminator to {clientSignature}: {username}, {discriminator}");

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
                        clients.TryTake(out clientSocket); // Remove from client list
                        Console.WriteLine($"{GetTimeStamp()} Client {clientSignature} disconnected.");
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
                    Console.WriteLine($"{GetTimeStamp()} Received message from {clientSignature} ({messageLength} bytes). Sending to all client...");
                    BroadcastMessageToAllClients(messageBuffer, clients, clientSignature);
                }
                catch (SocketException)
                {
                    // Handle any socket errors (like disconnects)
                    Console.WriteLine($"{GetTimeStamp()} Client {clientSignature} connection lost.");
                    clients.TryTake(out clientSocket);
                    break;
                }
            }
        }

        /// <summary>
        /// Take the message and broadcast it to all the clients
        /// </summary>
        /// <param name="messageBuffer"></param>
        /// <param name="clients"></param>
        static void BroadcastMessageToAllClients(byte[] messageBuffer, ConcurrentBag<Socket> clients, string senderSignature)
        {
            foreach (var client in clients)
            {
                SendMessageToClient(client, messageBuffer);
            }
            Console.WriteLine($"{GetTimeStamp()} Sent message from {senderSignature} to all clients");
        }

        /// <summary>
        /// Send message to individual client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="messageBuffer"></param>
        static void SendMessageToClient(Socket client, byte[] messageBuffer)
        {
            try
            {
                client.Send(messageBuffer);
            }
            catch (SocketException)
            {
                // Handle if a client is disconnected or unreachable
                Console.WriteLine($"Failed to send message to client {client.RemoteEndPoint}");
            }
        }

        static string GetTimeStamp()
        {
            return DateTime.Now.ToString("[yyyy/MM/dd HH:mm:ss]");
        }

        /// <summary>
        /// Random unique 4 digit string
        /// </summary>
        /// <returns></returns>
        static string CreateNewDiscriminator()
        {
            while (true)
            {
                Random random = new Random();
                int randomNumber = random.Next(1000, 10000); // Generates a number between 1000 and 9999
                string discriminator = randomNumber.ToString();
                if (discriminators.Contains(discriminator)) continue;
                return discriminator;
            }
        }
    }
}