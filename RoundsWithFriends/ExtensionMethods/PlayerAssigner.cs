using System;
using System.Linq;
using System.Collections;
using InControl;
using UnboundLib;
using Photon.Pun;
using SoundImplementation;
using UnityEngine;
using UnboundLib.Networking;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RWF
{
    // extensions to support multiple local players
    public static class PlayerAssignerExtensions
    {
        public static IEnumerator CreatePlayer(this PlayerAssigner instance, LobbyCharacter character, InputDevice inputDevice)
        {
            if ((bool)instance.GetFieldValue("waitingForRegisterResponse"))
            {
                yield break;
            }
            if (instance.players.Count < instance.maxPlayers)
            {
                if (!PhotonNetwork.OfflineMode && !PhotonNetwork.IsMasterClient)
                {
                    instance.GetComponent<PhotonView>().RPC("RPCM_RequestTeamAndPlayerID", RpcTarget.MasterClient, new object[]
                    {
                        PhotonNetwork.LocalPlayer.ActorNumber
                    });
                    instance.SetFieldValue("waitingForRegisterResponse", true);
                }
                while ((bool)instance.GetFieldValue("waitingForRegisterResponse"))
                {
                    yield return null;
                }
                if (!PhotonNetwork.OfflineMode)
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        instance.SetFieldValue("playerIDToSet", PlayerManager.instance.players.Count);
                        instance.SetFieldValue("teamIDToSet", character.teamID);
                    }
                }
                else
                {
                    instance.SetFieldValue("playerIDToSet", PlayerManager.instance.players.Count);
                    instance.SetFieldValue("teamIDToSet", character.teamID);
                }
                SoundPlayerStatic.Instance.PlayPlayerAdded();
                Vector3 position = Vector3.up * 100f;
                CharacterData component = PhotonNetwork.Instantiate(instance.playerPrefab.name, position, Quaternion.identity, 0, null).GetComponent<CharacterData>();

                if (inputDevice != null)
                {
                    component.input.inputType = GeneralInput.InputType.Controller;
                    component.playerActions = PlayerActions.CreateWithControllerBindings();
                }
                else
                {
                    component.input.inputType = GeneralInput.InputType.Keyboard;
                    component.playerActions = PlayerActions.CreateWithKeyboardBindings();
                }
                component.playerActions.Device = inputDevice;
                
                instance.players.Add(component);
                PlayerManager.RegisterPlayer(component.player);
                //component.player.AssignCharacter(character, (int)instance.GetFieldValue("playerIDToSet"));
                // assign character
                yield return SyncMethodStatic.SyncMethod(typeof(PlayerAssignerExtensions), nameof(PlayerAssignerExtensions.RPCA_AssignCharacter), null, component.view.ViewID, character, (int) instance.GetFieldValue("playerIDToSet"));

                yield break;
            }
            yield break;
        }
        [UnboundRPC]
        private static void RPCA_AssignCharacter(int viewID, LobbyCharacter character, int playerID)
        {
            PlayerAssigner.instance.StartCoroutine(PlayerAssignerExtensions.AssignCharacterCoroutine(viewID, character, playerID));
        }
        private static IEnumerator AssignCharacterCoroutine(int viewID, LobbyCharacter character, int playerID)
        {
            yield return new WaitUntil(() =>
            {
                return (PhotonView.Find(viewID) != null && PhotonView.Find(viewID).GetComponent<Player>() != null);
            });

            // only assign on all other clients
            //if (!PhotonView.Find(viewID).IsMine) { PhotonView.Find(viewID).GetComponent<Player>().AssignCharacter(character, playerID); }
            PhotonView.Find(viewID).GetComponent<Player>().AssignCharacter(character, playerID);

            NetworkingManager.RPC(typeof(PlayerAssignerExtensions), nameof(PlayerAssignerExtensions.CreatePlayerResponse), PhotonNetwork.LocalPlayer.ActorNumber, character);
        }

        [UnboundRPC]
        public static void CreatePlayerResponse(int respondingPlayer, LobbyCharacter targetedCharacter)
        {
            if (targetedCharacter.IsMine)
            {
                SyncMethodStatic.RemovePendingRequest(typeof(PlayerAssignerExtensions), respondingPlayer, nameof(PlayerAssignerExtensions.RPCA_AssignCharacter));
            }
        }
        
    }
}
