using Photon.Pun;
using System.Linq;
using UnityEngine;

namespace RWF
{
    public class LobbyCharacter
    {
        // class to store lobby player information before actual Rounds players are created
     
        public static LobbyCharacter GetLobbyCharacter(int uniqueID)
        {
            return PhotonNetwork.CurrentRoom.Players.Values.Select(p => p.GetProperty<LobbyCharacter[]>("players")).SelectMany(p => p).Where(p => p != null && p.uniqueID == uniqueID).FirstOrDefault();
        }
        
        public static object Deserialize(byte[] data)
        {
            if (PhotonNetwork.CurrentRoom == null || data == null || (int)data[6] == 1 || !PhotonNetwork.CurrentRoom.Players.ContainsKey((int) data[0]))
            {
                return null;
            }
            LobbyCharacter result = new LobbyCharacter(PhotonNetwork.CurrentRoom.Players[(int) data[0]], (int) data[1], (int)data[2])
            {
                teamID = (int) data[3],
                ready = (int) data[5] == 1
            };
            result.faceID = (int) data[4];
            return result;
        }

        public static byte[] Serialize(object lobbyCharacter)
        {
            if (lobbyCharacter == null) { return new byte[] { 0,0,0,0,0,0,1 }; }

            LobbyCharacter c = (LobbyCharacter) lobbyCharacter;
            return new byte[] { (byte)c.actorID, (byte)c.colorID, (byte)c.localID, (byte)c.teamID, (byte)c.faceID, (byte)(c.ready ? 1 : 0), 0};
        }

        public Photon.Realtime.Player networkPlayer;
        // unique IDs are purposely negative so that they can be easily differentiated from actorIDs
        public int uniqueID => -(this.actorID * RWFMod.instance.MaxCharactersPerClient + this.localID) - 1;
        public int colorID;
        public int teamID;
        public int faceID;
        public bool ready = false;
        public string NickName => this.localID == 0 ? networkPlayer.NickName : networkPlayer.NickName + $" {this.localID + 1}";
        public int actorID => this.networkPlayer.ActorNumber;
        public int localID;

        public bool IsMine => this.actorID == PhotonNetwork.LocalPlayer.ActorNumber;

        public LobbyCharacter(Photon.Realtime.Player networkPlayer, int colorID, int localID)
        {
            this.networkPlayer = networkPlayer;
            this.colorID = colorID;
            this.localID = localID;
            this.faceID = PlayerPrefs.GetInt("SelectedFace"+localID);
        }

        public void SetReady(bool ready)
        {
            this.ready = ready;
        }
    }
}
