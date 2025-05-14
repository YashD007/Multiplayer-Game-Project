using UnityEngine;
using Photon.Chat;
using Photon.Pun;
using ExitGames.Client.Photon;
using TMPro;

public class PhotonChatManager : MonoBehaviour, IChatClientListener
{
    public TMP_InputField chatInputField;
    public TextMeshProUGUI chatDisplay;
    public string chatChannel = "Lobby";

    private ChatClient chatClient;
    private bool isConnected = false;

    public void StartChat(string nickname)
    {
        if (chatClient != null && chatClient.CanChat)
            return;

        chatClient = new ChatClient(this);
        chatClient.Connect(
            PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat,
            "1.0",
            new AuthenticationValues(nickname)
        );

        chatInputField.onSubmit.AddListener(OnSubmitChat);
    }

    void Update()
    {
        if (chatClient != null)
            chatClient.Service();
    }

    private void OnSubmitChat(string text)
    {
        if (!string.IsNullOrWhiteSpace(text) && isConnected)
        {
            chatClient.PublishMessage(chatChannel, text);
            chatInputField.text = "";
            chatInputField.ActivateInputField();
        }
    }

    public void OnConnected()
    {
        chatClient.Subscribe(new string[] { chatChannel });
        Debug.Log("✅ Connected to Photon Chat");
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        isConnected = true;
        Debug.Log("✅ Subscribed to chat channel");
    }

    public void OnDisconnected() => Debug.Log("❌ Disconnected from Photon Chat");

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
            chatDisplay.text += $"\n<b>{senders[i]}</b>: {messages[i]}";
    }

    public void OnChatStateChange(ChatState state) { }
    public void OnPrivateMessage(string sender, object message, string channelName) { }
    public void OnUnsubscribed(string[] channels) { }
    public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }
    public void DebugReturn(DebugLevel level, string message) { }
    public void OnUserSubscribed(string channel, string user) { }
    public void OnUserUnsubscribed(string channel, string user) { }
}
