namespace EBookLibraryProtocol.Server
{
    // тут буду хранить откусанные куски зашифрованной части

    static class DataStore
    {
        // словарь: ключ «bookId:userId:layer» → IV-байты
        private static readonly Dictionary<string, byte[]> serverIvStore
            = new Dictionary<string, byte[]>();

        // список для блокчейна, в формате хэша (?)
        private static readonly List<(string Hash, string PrevHash, DateTime Time)> blockchain
            = new List<(string, string, DateTime)>();

        // сохраняет IV для конкретного слоя
        public static void StoreIv(string bookId, string userId, int layer, byte[] iv)
        {
            string key = MakeKey(bookId, userId, layer);
            serverIvStore[key] = iv;
        }

        // пытается вернуть IV для заданного слоя
        public static bool TryGetIv(string bookId, string userId, int layer, out byte[] iv)
        {
            string key = MakeKey(bookId, userId, layer);
            return serverIvStore.TryGetValue(key, out iv);
        }

        // очистка выданного откусанного куска
        public static void RemoveIv(string bookId, string userId, int layer)
        {
            string key = MakeKey(bookId, userId, layer);
            serverIvStore.Remove(key);
        }

        // + блокчейн
        public static void AddBlockchainRecord(string hash, string prevHash)
        {
            blockchain.Add((hash, prevHash, DateTime.UtcNow));
        }

        // возвращает предыдущий хэш (0 если блокчейна еще нет)
        public static string GetPreviousHash()
        {
            if (blockchain.Count == 0) return "0";
            return blockchain[^1].Hash;
        }

        private static string MakeKey(string bookId, string userId, int layer)
        {
            return $"{bookId}:{userId}:{layer}";
        }
    }
}

