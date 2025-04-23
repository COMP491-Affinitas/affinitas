using System;
using System.Text;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;

[Serializable]
public class ClientResponse
{
    public string playerResponse;
    public bool requestQuest;
    public string error;
    public string messageId;
}

[Serializable]
public class ServerResponse
{
    public int npcId;
    public int affinitasChange;
    public string summary;
    public string npcResponse;
    public string error;
    public string messageId;
}

public class ServerConnection : MonoBehaviour
{
    public static ServerConnection Instance { get; private set; }

    public const string serverURL = "http://localhost:8000/hello";
    public static readonly HttpClient client;

    public HttpResponseMessage response;


    public async Task<ServerResponse> SendAndGetMessageFromServer(string messageToSend, bool requestQuest)
    {
        // Create message with info
        ClientResponse message = new ClientResponse
        {
            playerResponse = messageToSend,
            requestQuest = false,
            error = "",
            messageId = Guid.NewGuid().ToString()
        };

        // If player clicks Get Request button
        if (requestQuest)
        {
            message.requestQuest = true;
        }

        // Change to JSON
        StringContent reqBody = new StringContent(
            JsonUtility.ToJson(message),
            Encoding.UTF8,
            "application/json"
        );

        ServerResponse serverResponse = null;

        try
        {
            // Send player input text and wait for message
            response = await client.PostAsync(serverURL, reqBody);
            string result = await response.Content.ReadAsStringAsync();

            Debug.Log($"Status Code: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                serverResponse = JsonUtility.FromJson<ServerResponse>(result);
            }
            else
            {
                Debug.LogError($"Request failed: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception occurred: {ex.Message}");
        }

        return serverResponse;

    }
}
