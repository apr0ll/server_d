using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace EBookLibraryProtocol.Server
{
    public static class ClientHandler
    {
        private static readonly Dictionary<(string, string, int), byte[]> ServerParts = new();
        private static readonly List<(string Hash, string PrevHash, DateTime Timestamp)> Blockchain = new();

        public static void Handle(TcpClient client)
        {
            using NetworkStream stream = client.GetStream();
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            string header = reader.ReadString();
            Console.WriteLine($"Получен заголовок от клиента: {header}");
            if (!RequestParser.TryParseRequest(header, out string bookId, out string userId, out int n))
            {
                Console.WriteLine("Ошибка: Некорректный формат запроса.");
                writer.Write("ERROR: некорректный запрос");
                return;
            }

            Console.WriteLine($"Запрос успешно разобран: bookId={bookId}, userId={userId}, слоев={n}");

            // Получаем публичный ключ клиента
            int publicKeyLen = reader.ReadInt32();
            Console.WriteLine($"Получаю публичный ключ клиента, длина: {publicKeyLen} байт");
            byte[] publicKeyBytes = reader.ReadBytes(publicKeyLen);
            Console.WriteLine($"Публичный ключ клиента получен, длина: {publicKeyBytes.Length} байт");

            using var rsaServer = RSA.Create();
            rsaServer.ImportRSAPublicKey(publicKeyBytes, out _);

            string BOOK_TEXT = "Пример текста электронной книги.";
            Console.WriteLine($"Текст книги для шифрования: {BOOK_TEXT}");

            // Шифруем текст публичным ключом клиента
            byte[] bookBytes = Encoding.UTF8.GetBytes(BOOK_TEXT);
            byte[] cipherText = rsaServer.Encrypt(bookBytes, RSAEncryptionPadding.OaepSHA256);
            Console.WriteLine($"Текст зашифрован публичным ключом клиента, длина: {cipherText.Length} байт");

            // Генерируем ключи K1, K2, ..., Kn
            DateTime timestamp = DateTime.UtcNow;
            var layerKeys = new List<byte[]>();
            for (int i = 1; i <= n; i++)
            {
                byte[] key = CryptoUtils.GenerateLayerKey(bookId, userId, i, n, timestamp);
                layerKeys.Add(key);
                Console.WriteLine($"K{i} сгенерирован: {BitConverter.ToString(key).Replace("-", "")}");
            }

            // Шифруем cipherText для каждого слоя
            var encryptedLayers = new List<byte[]>();
            for (int i = 1; i <= n; i++)
            {
                byte[] e_i = CryptoUtils.EncryptAesCbc(cipherText, layerKeys[i - 1]);
                Console.WriteLine($"E{i} зашифрован с K{i}, длина: {e_i.Length} байт");
                encryptedLayers.Add(e_i);
            }

            // Разделяем на userPart и tail
            var userParts = new List<byte[]>();
            for (int i = 1; i <= n; i++)
            {
                int cut = (int)(encryptedLayers[i - 1].Length * 0.9);
                byte[] userPart = encryptedLayers[i - 1][..cut];
                byte[] tail = encryptedLayers[i - 1][cut..];
                ServerParts[(bookId, userId, i)] = tail;
                userParts.Add(userPart);
                Console.WriteLine($"E{i} разделён: tail сохранён, длина={tail.Length} байт, userPart, длина={userPart.Length} байт");
            }

            // Формируем бутер E_n || E_{n-1} || ... || E_1
            byte[] sandwich = userParts[0];
            for (int i = 1; i < userParts.Count; i++)
            {
                sandwich = sandwich.Concat(userParts[i]).ToArray();
            }
            Console.WriteLine($"Сформирован бутерброд E{n}||...||E1, длина: {sandwich.Length} байт");

            // Шифруем ключи для клиента
            var encryptedLayerKeys = new List<byte[]>();
            for (int i = 0; i < layerKeys.Count; i++)
            {
                byte[] encryptedKey = rsaServer.Encrypt(layerKeys[i], RSAEncryptionPadding.OaepSHA256);
                encryptedLayerKeys.Add(encryptedKey);
                Console.WriteLine($"K{i + 1} зашифрован для клиента, длина: {encryptedKey.Length} байт");
            }

            // Отправляем клиенту
            Console.WriteLine("Отправляем клиенту подтверждение, бутерброд и ключи...");
            writer.Write("OK");
            writer.Write(sandwich.Length);
            writer.Write(sandwich);

            // Отправляем зашифрованные ключи
            Console.WriteLine("Отправляем зашифрованные ключи...");
            writer.Write("KEYS");
            writer.Write(n); // Количество ключей
            foreach (var encryptedKey in encryptedLayerKeys)
            {
                writer.Write(encryptedKey.Length);
                writer.Write(encryptedKey);
            }

            // Обрабатываем запросы tail
            for (int i = n; i >= 1; i--)
            {
                string req = reader.ReadString();
                Console.WriteLine($"Получен запрос слоя от клиента: {req}");
                if (!RequestParser.TryParseLayerRequest(req, i))
                {
                    Console.WriteLine($"Ошибка: Неверный формат запроса слоя {i}");
                    writer.Write("ERROR: неверный LAYER запрос");
                    return;
                }

                if (!ServerParts.TryGetValue((bookId, userId, i), out byte[] tail))
                {
                    Console.WriteLine($"Ошибка: Tail для слоя {i} не найден на сервере");
                    writer.Write("ERROR: tail не найден");
                    return;
                }

                Console.WriteLine($"Отправляем tail для слоя {i}, длина: {tail.Length} байт");
                writer.Write(tail.Length);
                writer.Write(tail);

                Console.WriteLine($"Вычисляем хэш для записи в блокчейн...");
                string hash = CryptoUtils.ComputeHash($"{bookId}{userId}{i}");
                string prev = Blockchain.Count > 0 ? Blockchain[^1].Hash : "0";
                Blockchain.Add((hash, prev, DateTime.UtcNow));
                ServerParts.Remove((bookId, userId, i));
                Console.WriteLine($"Tail для слоя {i} выдан, запись добавлена в блокчейн: хэш={hash}, предыдущий хэш={prev}");
            }

            Console.WriteLine("Сессия клиента завершена.\n");
        }
    }
}