using System;
using System.Text;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

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
public class LoadSaveRequest : BaseRequest
{
    public string save_id;
}

[Serializable]
public class GetSavesRequest : BaseRequest
{
}
[Serializable]
public class GetSavesResponse : BaseRequest
{
    public List<Save> saves;
}
[Serializable]
public class Save
{
    public string save_id;
    public string name;
    public string saved_at;
}
[Serializable]
public class SaveRequest : BaseRequest
{
    public string name;
    public string shadow_save_id;
}
[Serializable]
public class SaveResponse : BaseRequest
{
    public string save_id;
    public string name;
    public string saved_at;
}
[Serializable]
public class QuitRequest : BaseRequest
{
    public string save_id;
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
public class TakeItemRequest : BaseRequest
{
    public string item_name;
    public string shadow_save_id;
}

[Serializable]
public class GiveItemRequest : BaseRequest
{
    public string quest_id;
    public string shadow_save_id;
}
[Serializable]
public class GiveItemResponse : BaseResponse
{
    public string response;
}

[Serializable]
public class PlayerRequest : BaseRequest
{
    public string role;     //"system" or "user"
    public string shadow_save_id;
    public string content;
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
}

public class ServerConnection : MonoBehaviour
{
    public static ServerConnection Instance { get; private set; }

    const string serverURL = "https://affinitas-pr-16.onrender.com";

    static HttpClient client = new HttpClient();

    public bool canSendMessage = true;
    private bool clientDisposed = false;

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
        if (method == null)
            method = HttpMethod.Post;

        var requestMessage = new HttpRequestMessage(method, serverURL + directoryPath);

        if (method == HttpMethod.Post || method == HttpMethod.Delete)
        {
            requestMessage.Content = new StringContent(
                JsonConvert.SerializeObject(message),  Encoding.UTF8, "application/json");
        }

        Debug.Log("Sending message to server with x-client-uuid: " + GameManager.Instance.playerId);
        Debug.Log("Sending message to server with shadow-save-id: " + GameManager.Instance.shadowSaveId);

        // Set the header x-client-uuid
        if (!string.IsNullOrEmpty(GameManager.Instance.playerId)) {
            requestMessage.Headers.Add("x-client-uuid", GameManager.Instance.playerId);
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
                    serverResponse = (BaseResponse)JsonConvert.DeserializeObject(result, typeof(BaseResponse));
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

}