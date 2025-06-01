using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EBookLibraryProtocol.Server
{
    class Program
    {
        public const int Port = 9000;
        public const string EncodingName = "UTF-8";

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.GetEncoding(EncodingName);
            Console.WriteLine("=== Сервер протокола электронной библиотеки ===");
            var listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            Console.WriteLine($"Сервер запущен, слушаем порт {Port}...\n");

            while (true)
            {
                Console.WriteLine("Ожидаем подключения клиента...");
                using var client = listener.AcceptTcpClient();
                Console.WriteLine("Клиент подключился.\n");
                ClientHandler.Handle(client);
                Console.WriteLine("Сессия с клиентом завершена.\n");
            }
        }
    }
}
