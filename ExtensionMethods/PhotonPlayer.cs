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
    }
}
