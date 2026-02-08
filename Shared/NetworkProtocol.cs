using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace invaders.Shared
{
    public static class NetworkProtocol
    {
        private static readonly byte[] PACKET_HEADER = { 0x49, 0x4E, 0x56 };

        public static byte[] Serialize<T>(T obj)
        {
            if (obj == null) return Array.Empty<byte>();

            MemoryStream ms = new MemoryStream();

            ms.Write(PACKET_HEADER, 0, PACKET_HEADER.Length);

            byte typeId = GetTypeId<T>();
            ms.WriteByte(typeId);

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, obj);

            return ms.ToArray();
        }

        public static T Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length < PACKET_HEADER.Length + 1)
                return default(T);

            MemoryStream ms = new MemoryStream(data);

            byte[] header = new byte[PACKET_HEADER.Length];
            ms.Read(header, 0, PACKET_HEADER.Length);

            for (int i = 0; i < PACKET_HEADER.Length; i++)
            {
                if (header[i] != PACKET_HEADER[i])
                    throw new InvalidDataException("Invalid packet header");
            }

            byte typeId = (byte)ms.ReadByte();
            byte expectedTypeId = GetTypeId<T>();

            if (typeId != expectedTypeId)
                throw new InvalidDataException("Type mismatch: expected " + expectedTypeId + ", got " + typeId);

            BinaryFormatter formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(ms);
        }

        private static byte GetTypeId<T>()
        {
            string typeName = typeof(T).Name;

            if (typeName == "GameState") return 1;
            if (typeName == "InputPacket") return 2;
            if (typeName == "LoginRequest") return 3;
            if (typeName == "LoginResponse") return 4;
            if (typeName == "PlayerData") return 5;
            if (typeName == "EnemyData") return 6;
            if (typeName == "ProjectileData") return 7;
            if (typeName == "ResetRequest") return 8;

            return 255;
        }

        public static byte[] PackWithLength(byte[] data)
        {
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            byte[] result = new byte[4 + data.Length];
            Array.Copy(lengthBytes, 0, result, 0, 4);
            Array.Copy(data, 0, result, 4, data.Length);
            return result;
        }

        public static byte[] ReadWithLength(Stream stream)
        {
            byte[] lengthBytes = new byte[4];
            int bytesRead = stream.Read(lengthBytes, 0, 4);
            if (bytesRead < 4) return null;

            int length = BitConverter.ToInt32(lengthBytes, 0);
            if (length <= 0 || length > 1024 * 1024)
                return null;

            byte[] data = new byte[length];
            int totalRead = 0;
            while (totalRead < length)
            {
                bytesRead = stream.Read(data, totalRead, length - totalRead);
                if (bytesRead == 0) break;
                totalRead += bytesRead;
            }

            return totalRead == length ? data : null;
        }

        public static bool IsValidPacket(byte[] data)
        {
            if (data == null || data.Length < PACKET_HEADER.Length)
                return false;

            for (int i = 0; i < PACKET_HEADER.Length; i++)
            {
                if (data[i] != PACKET_HEADER[i])
                    return false;
            }
            return true;
        }
    }
}