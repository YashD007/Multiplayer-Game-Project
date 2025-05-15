using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public GameObject player;
    public Transform[] spawnPoints;
    // public PhotonChatManager chatManager; // 👈 Drag your ChatManager here in the Inspector

    void Start()
    {
        PhotonNetwork.NickName = "Player" + Random.Range(1000, 9999);
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        RoomOptions options = new RoomOptions { MaxPlayers = 10 };
        PhotonNetwork.JoinOrCreateRoom("Test", options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        int index = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        if (index >= spawnPoints.Length) index = 0;

        Vector3 spawnPosition = spawnPoints[index].position + Vector3.up * 1.5f;
        PhotonNetwork.Instantiate(player.name, spawnPosition, Quaternion.identity);

        // ✅ Start chat after room is joined
        // chatManager.StartChat(PhotonNetwork.NickName);
    }
}
