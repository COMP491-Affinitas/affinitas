using System;
using System.Text;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

[Serializable]
public abstract class BaseRequest
{
}
[Serializable]
public abstract class BaseResponse
{
}

[Serializable]
public class UuidRequest : BaseRequest
{
    public string x_client_uuid;
}
[Serializable]
public class UuidResponse : BaseResponse
{
    public string uuid;
}

[Serializable]
public class EndingRequest : BaseRequest
{
    public string shadow_save_id;
}
[Serializable]
public class EndingResponse : BaseResponse
{
    public string ending;
}

[Serializable]
public class QuestRequest : BaseRequest
{
    public string shadow_save_id;
}
[Serializable]
public class QuestEntry
{
    public string quest_id;
    public string response;
}
[Serializable]
public class QuestListResponse : BaseResponse
{
    public List<QuestEntry> quests;
}

[Serializable]
public class QuestCompleteRequest : BaseRequest
{
    public string quest_id;
    public string shadow_save_id;
}
[Serializable]
public class QuestCompleteResponse : BaseResponse
{
    public int affinitas;
}


[Serializable]
public class PlayerRequest : BaseRequest
{
    public string role;     //"system" or "user"
    public string shadow_save_id;
    public string content;

    public PlayerRequest(string role, string shadow_save_id, string content)
    {
        this.role = role;
        this.shadow_save_id = shadow_save_id;
        //this.requestId = requestId;
        this.content = content;
    }
}

[Serializable]
public class NpcResponse : BaseResponse
{
    public string response;
    public int affinitas_new;
    public List<string> completed_quests;
}

//  JSON body should be sent to POST/game/quit endpoint; becasue it expects save_id not x_client_uuid
[Serializable]
public class SerializableSaveId
{
    public string save_id;

    public SerializableSaveId(string saveId)
    {
        save_id = saveId;
    }
}

public class ServerConnection : MonoBehaviour
{
    public static ServerConnection Instance { get; private set; }

    const string serverURL = "https://affinitas-pr-16.onrender.com";
    //static readonly HttpClient client = new HttpClient(); 
    static HttpClient client = new HttpClient();

    public bool canSendMessage = true;
    private bool clientDisposed = false;

    //HttpResponseMessage response;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // To be called from other classes in game when they are done with current server communication
    public void OnServerMessageReceived()
    {
        canSendMessage = true;
    }

    // It is automatically called by Unity when application is quitting.
    // Used to ensure that the HTTP client is properly disposed and server connection is closed.
    private async void OnApplicationQuit()
    {
        if (!string.IsNullOrEmpty(GameManager.Instance.shadowSaveId))
        {
            await SendQuitGameRequest(GameManager.Instance.shadowSaveId);
        }

        CloseServerConnection();
    }

    // Disposes of the HTTP client to release network resources and close any open connections.
    // This prevents potential memory leaks or lingering connections after the application exits.
    public void CloseServerConnection()
    {
        if (!clientDisposed)
        {
            client.Dispose();
            clientDisposed = true;
            Debug.Log("Server connection closed on quit.");
        }
    }

    // Send and Get Generic Response from Server
    public async Task<BaseResponse> SendAndGetMessageFromServer<BaseRequest, BaseResponse>(BaseRequest message, string directoryPath, HttpMethod method = null)
    {
        
        if (method == null) method = HttpMethod.Post;
        var requestMessage = new HttpRequestMessage(method, serverURL + directoryPath);

        if (method == HttpMethod.Post) {
            requestMessage.Content = new StringContent(
                JsonUtility.ToJson(message), Encoding.UTF8, "application/json");
        }

        // Set the header x-client-uuid
        if (!string.IsNullOrEmpty(GameManager.Instance.gameId)) {
            requestMessage.Headers.Add("x-client-uuid", GameManager.Instance.gameId);
        }

        if (directoryPath.StartsWith("/npcs/")) {
            requestMessage.Headers.Add("shadow-save-id", GameManager.Instance.shadowSaveId);
        }

        BaseResponse serverResponse = default;
        try
        {
            // Send request and wait for response
            var response = await client.SendAsync(requestMessage);
            
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    Debug.Log("204 status code returned.");
                else
                {
                    string result = await response.Content.ReadAsStringAsync();
                    // Change back from JSON
                    Debug.Log("Got message from server.");
                    serverResponse = JsonUtility.FromJson<BaseResponse>(result);
                }  
            }
            else
                Debug.LogError($"Request failed: {response.StatusCode} - {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception occurred: {ex.Message}");
        }
        return serverResponse;
    }

    public async Task SendQuitGameRequest(string saveId)
    {
        if (string.IsNullOrEmpty(GameManager.Instance.gameId))
        {
            Debug.LogError("Game ID (UUID) is missing. Cannot send quit request.");
            return;
        }

        var requestJson = JsonUtility.ToJson(new SerializableSaveId(saveId));
        var request = new HttpRequestMessage(HttpMethod.Post, serverURL + "/game/quit")
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("X-Client-UUID", GameManager.Instance.gameId);

        try
        {
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                Debug.Log("Quit request successful.");
            }
            else
            {
                Debug.LogWarning($"Quit request failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception during quit request: {ex.Message}");
        }
    }
}