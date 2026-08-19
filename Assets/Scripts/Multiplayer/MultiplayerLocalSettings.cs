using System;
using UnityEngine;

namespace Rocket.Multiplayer
{
    public static class MultiplayerLocalSettings
    {
        private const string PlayerNameKey = "rocket.multiplayer.player_name";
        private const string AdvertisedAddressKey = "rocket.multiplayer.advertised_address";
        private const string MobileModeKey = "rocket.multiplayer.mobile_mode";

        public static string PlayerName
        {
            get
            {
                string value = PlayerPrefs.GetString(PlayerNameKey, "Player_01");
                return string.IsNullOrWhiteSpace(value) ? "Player_01" : value.Trim();
            }
            set
            {
                PlayerPrefs.SetString(PlayerNameKey, string.IsNullOrWhiteSpace(value) ? "Player_01" : value.Trim());
                PlayerPrefs.Save();
            }
        }

        public static string AdvertisedAddress
        {
            get => PlayerPrefs.GetString(AdvertisedAddressKey, string.Empty).Trim();
            set
            {
                PlayerPrefs.SetString(AdvertisedAddressKey, value?.Trim() ?? string.Empty);
                PlayerPrefs.Save();
            }
        }

        public static MobileInputUI.DisplayMode MobileDisplayMode
        {
            get
            {
                int stored = PlayerPrefs.GetInt(MobileModeKey, (int)MobileInputUI.DisplayMode.Auto);
                return Enum.IsDefined(typeof(MobileInputUI.DisplayMode), stored)
                    ? (MobileInputUI.DisplayMode)stored
                    : MobileInputUI.DisplayMode.Auto;
            }
            set
            {
                PlayerPrefs.SetInt(MobileModeKey, (int)value);
                PlayerPrefs.Save();
            }
        }
    }
}
