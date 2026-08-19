using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Rocket.Multiplayer
{
    public static class RoomKeyCodec
    {
        // 32 symbols are required because the codec packs/unpacks 5-bit values.
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ234567890";

        public static bool TryEncode(string address, ushort port, NetworkRoomMode mode, out string roomKey)
        {
            roomKey = string.Empty;

            if (!IPAddress.TryParse(address, out IPAddress parsedAddress) || parsedAddress.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            byte[] addressBytes = parsedAddress.GetAddressBytes();
            byte[] payload = new byte[7];
            payload[0] = (byte)mode;
            payload[1] = addressBytes[0];
            payload[2] = addressBytes[1];
            payload[3] = addressBytes[2];
            payload[4] = addressBytes[3];
            payload[5] = (byte)(port >> 8);
            payload[6] = (byte)(port & 0xFF);

            roomKey = EncodePayload(payload);
            return true;
        }

        public static bool TryDecode(string roomKey, out string address, out ushort port, out NetworkRoomMode mode)
        {
            address = string.Empty;
            port = 0;
            mode = NetworkRoomMode.Lan;

            if (!TryDecodePayload(roomKey, out byte[] payload) || payload.Length != 7)
            {
                return false;
            }

            mode = payload[0] == (byte)NetworkRoomMode.Online ? NetworkRoomMode.Online : NetworkRoomMode.Lan;
            address = $"{payload[1]}.{payload[2]}.{payload[3]}.{payload[4]}";
            port = (ushort)((payload[5] << 8) | payload[6]);
            return true;
        }

        private static string EncodePayload(byte[] payload)
        {
            ulong accumulator = 0;
            for (int i = 0; i < payload.Length; i++)
            {
                accumulator = (accumulator << 8) | payload[i];
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 12; i++)
            {
                int index = (int)(accumulator & 31UL);
                if (index < 0 || index >= Alphabet.Length)
                {
                    throw new InvalidOperationException($"Room key alphabet index {index} is out of range.");
                }

                builder.Insert(0, Alphabet[index]);
                accumulator >>= 5;
            }

            string raw = builder.ToString().TrimStart('A');
            string padded = string.IsNullOrEmpty(raw) ? "A" : raw;
            return InsertDashes(padded);
        }

        private static bool TryDecodePayload(string roomKey, out byte[] payload)
        {
            payload = Array.Empty<byte>();

            if (string.IsNullOrWhiteSpace(roomKey))
            {
                return false;
            }

            string sanitized = roomKey.Replace("-", string.Empty).Trim().ToUpperInvariant();
            if (sanitized.Length == 0 || sanitized.Length > 12)
            {
                return false;
            }

            ulong accumulator = 0;
            for (int i = 0; i < sanitized.Length; i++)
            {
                int index = Alphabet.IndexOf(sanitized[i]);
                if (index < 0)
                {
                    return false;
                }

                accumulator = (accumulator << 5) | (uint)index;
            }

            int totalBits = sanitized.Length * 5;
            int targetBits = 56;
            if (totalBits < targetBits)
            {
                accumulator <<= (targetBits - totalBits);
            }

            payload = new byte[7];
            for (int i = payload.Length - 1; i >= 0; i--)
            {
                payload[i] = (byte)(accumulator & 0xFF);
                accumulator >>= 8;
            }

            return true;
        }

        private static string InsertDashes(string raw)
        {
            if (raw.Length <= 4)
            {
                return raw;
            }

            if (raw.Length <= 8)
            {
                return raw.Insert(4, "-");
            }

            return raw.Insert(4, "-").Insert(9, "-");
        }
    }
}
