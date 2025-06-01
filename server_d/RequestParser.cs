using System;

namespace EBookLibraryProtocol.Server
{     
    static class RequestParser
    { 
        public static bool TryParseRequest(string line, out string bookId, out string userId, out int layersCount)
        {
            bookId = null;
            userId = null;
            layersCount = 0;

            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 4 || tokens[0] != "REQUEST") return false;
            if (!int.TryParse(tokens[3], out layersCount) || layersCount <= 0) return false;
            bookId = tokens[1];
            userId = tokens[2];
            return true;
        }
        
        // ждет строку "LAYER i" и проверяет, что i соответствует ожидаемому слою.
         
        public static bool TryParseLayerRequest(string line, int expectedLayer)
        {
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 2 || tokens[0] != "LAYER") return false;
            if (!int.TryParse(tokens[1], out int parsed) || parsed != expectedLayer) return false;
            return true;
        }
    }
}
