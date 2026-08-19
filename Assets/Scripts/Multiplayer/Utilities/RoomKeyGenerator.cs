using System.Collections.Generic;
using System.Text;
using Rocket.Multiplayer.Core;
using UnityEngine;

namespace Rocket.Multiplayer.Utilities
{
    /// <summary>
    /// Generates short, human-readable room keys from an unambiguous alphabet.
    /// Keys are opaque random strings, never an encoded address - resolving a key
    /// to connection info is the room service's job, not this generator's.
    /// </summary>
    public static class RoomKeyGenerator
    {
        public static string Generate(MultiplayerConfig config)
        {
            string alphabet = string.IsNullOrEmpty(config.RoomKeyAlphabet)
                ? "ABCDEFGHJKMNPQRSTUVWXYZ23456789"
                : config.RoomKeyAlphabet;
            int length = Mathf.Max(4, config.RoomKeyLength);

            StringBuilder builder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                int index = Random.Range(0, alphabet.Length);
                builder.Append(alphabet[index]);
            }

            return builder.ToString();
        }

        public static string GenerateUnique(MultiplayerConfig config, IEnumerable<string> knownKeys)
        {
            HashSet<string> known = new HashSet<string>(knownKeys ?? System.Array.Empty<string>());
            string key;
            int attempts = 0;
            do
            {
                key = Generate(config);
                attempts++;
            }
            while (known.Contains(key) && attempts < 20);

            return key;
        }
    }
}
