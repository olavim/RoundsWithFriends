using ExitGames.Client.Photon;
using Photon.Pun;
using BepInEx.Logging;

namespace RWF
{
    class NetworkManager : MonoBehaviourPunCallbacks
    {
        private ManualLogSource logger;

        private void Awake() {
            logger = new ManualLogSource("RWF::Network");
            BepInEx.Logging.Logger.Sources.Add(logger);

            logger.LogInfo("initialized");
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) {
            var teamSizeKey = RWFMod.GetCustomPropertyKey("teamSize");

            if (propertiesThatChanged.ContainsKey(teamSizeKey)) {
                logger.LogInfo("TeamSize changed");
                RWFMod.instance.SetTeamSize((int) propertiesThatChanged[teamSizeKey]);
            }
        }
    }
}
