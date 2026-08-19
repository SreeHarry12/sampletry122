using UnityEngine;

namespace Rocket.Multiplayer.Utilities
{
    /// <summary>
    /// Provides a fallback "Player_XX" style display name when the user hasn't entered one.
    /// </summary>
    public static class PlayerNameGenerator
    {
        public static string GenerateFallbackName()
        {
            int suffix = Random.Range(1, 100);
            return $"Player_{suffix:00}";
        }

        public static string ResolveName(string enteredName)
        {
            return string.IsNullOrWhiteSpace(enteredName) ? GenerateFallbackName() : enteredName.Trim();
        }
    }
}
