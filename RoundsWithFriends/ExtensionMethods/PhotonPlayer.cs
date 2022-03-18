namespace RWF
{
    public static class PhotonPlayerExtensions
    {
        public static void SetProperty(this Photon.Realtime.Player instance, string key, object value) {
            var propKey = RWFMod.GetCustomPropertyKey(key);
            var props = instance.CustomProperties;

            if (!props.ContainsKey(propKey)) {
                props.Add(propKey, value);
            } else {
                props[propKey] = value;
            }

            instance.SetCustomProperties(props);
        }

        public static T GetProperty<T>(this Photon.Realtime.Player instance, string key) {
            var propKey = RWFMod.GetCustomPropertyKey(key);
            var props = instance.CustomProperties;

            if (!props.ContainsKey(propKey)) {
                return default;
            }

            return (T)props[propKey];
        }

        public static void SetModded(this Photon.Realtime.Player instance) {
            instance.SetProperty("modded", true);
        }

        public static bool IsModded(this Photon.Realtime.Player instance) {
            return instance.GetProperty<bool>("modded");
        }

        private static readonly string charkey = RWFMod.GetCustomPropertyKey("characters");
        public static LobbyCharacter[] GetCharacters(this Photon.Realtime.Player instance)
        {
            return instance.GetProperty<LobbyCharacter[]>(PhotonPlayerExtensions.charkey);
        }
        public static LobbyCharacter GetCharacter(this Photon.Realtime.Player instance, int localID)
        {
            LobbyCharacter[] characters = instance.GetCharacters();
            if (characters.Length > localID)
            {
                return instance.GetCharacters()[localID];
            }
            else
            {
                return null;
            }
        }
        public static void SetCharacters(this Photon.Realtime.Player instance, LobbyCharacter[] characters)
        {
            instance.SetProperty(PhotonPlayerExtensions.charkey, characters);
        }
        public static void SetCharacter(this Photon.Realtime.Player instance, LobbyCharacter character)
        {
            var props = instance.CustomProperties;

            if (!props.ContainsKey(PhotonPlayerExtensions.charkey))
            {
                instance.SetProperty(PhotonPlayerExtensions.charkey, new LobbyCharacter[RWFMod.MaxCharactersPerClientHardLimit]);
            }

            LobbyCharacter[] characters = instance.GetCharacters();
            characters[character.localID] = character;

            instance.SetCharacters(characters);
        }
    }
}
