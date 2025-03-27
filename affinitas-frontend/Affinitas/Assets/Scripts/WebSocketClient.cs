using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class WebSocketClient : MonoBehaviour
{
    [Serializable]
    public class MyMessage
    {
        public string message;
        public string error;
        public string id;
    }

    private ClientWebSocket client;
    private CancellationTokenSource cts = new CancellationTokenSource();
    private string webSocketUrl = "ws://localhost:8888/websocket";

    async void Start()
    {
        await ConnectWebSocket();
    }

    async Task ConnectWebSocket()
    {
        client = new ClientWebSocket();
        Uri serverUri = new Uri(webSocketUrl);

        try
        {
            await client.ConnectAsync(serverUri, cts.Token);
            Debug.Log("Connected to the WebSocket server at: " + webSocketUrl);

            MyMessage message = new MyMessage 
            { 
                message = "Hello, server!", 
                error = "", 
                id = Guid.NewGuid().ToString() 
            };

            string jsonToSend = JsonUtility.ToJson(message);
            Debug.Log("Sending JSON: " + jsonToSend);

            byte[] bytesToSend = Encoding.UTF8.GetBytes(jsonToSend);
            await client.SendAsync(new ArraySegment<byte>(bytesToSend), WebSocketMessageType.Text, true, cts.Token);

            byte[] receiveBuffer = new byte[1024];
            WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cts.Token);
            string receivedJson = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
            Debug.Log("Received JSON: " + receivedJson);

            try
            {
                MyMessage receivedMessage = JsonUtility.FromJson<MyMessage>(receivedJson);
                Debug.Log("Deserialized Message:");
                Debug.Log("  message: " + receivedMessage.message);
                Debug.Log("  error: " + receivedMessage.error);
                Debug.Log("  id: " + receivedMessage.id);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error during deserialization: " + ex.Message);
            }

            // Close the WebSocket connection.
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cts.Token);
            Debug.Log("WebSocket connection closed.");
        }
        catch (Exception ex)
        {
            Debug.LogError("WebSocket error: " + ex.Message);
        }
    }

    private void OnDestroy()
    {
        cts.Cancel();
        client?.Dispose();
    }
}