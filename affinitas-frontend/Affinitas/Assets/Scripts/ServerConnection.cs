using System;
using System.Text;
using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Networking;

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
public class DeleteSaveRequest : BaseRequest
{   
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
public class DayNoInfoRequest : BaseRequest
{
    public string shadow_save_id;
}
[Serializable]
public class QuitRequest : BaseRequest
{ 
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
    public string item_name;
    public string shadow_save_id;
}
[Serializable]
public class GiveItemResponse : BaseResponse
{
    public int affinitas_new;
    public string response;
    public List<string> completed_quests;
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

    const string serverURL = "https://affinitas.onrender.com"; 
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

    public async Task<TResponse> SendAndGetMessageFromServer<TRequest, TResponse>(TRequest message, string directoryPath, string method = "POST")
    {
        string url = serverURL + directoryPath;
        string json = JsonConvert.SerializeObject(message);

        UnityWebRequest request;

        if (method == "GET")
        {
            request = UnityWebRequest.Get(url);
        }
        else if (method == "DELETE")
        {
            request = UnityWebRequest.Delete(url);
        }
        else
        {
            request = new UnityWebRequest(url, method);
            byte[] jsonToSend = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
        }

        // Set headers
        if (!string.IsNullOrEmpty(GameManager.Instance.playerId))
            request.SetRequestHeader("x-client-uuid", GameManager.Instance.playerId);

        if (directoryPath.StartsWith("/npcs/"))
            request.SetRequestHeader("shadow-save-id", GameManager.Instance.shadowSaveId);

        // Send request
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            await Task.Yield();


        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Request failed: {request.error}");
            return default;
        }

        if (request.responseCode == 204)
        {
            Debug.Log("204 No Content returned.");
            return default;
        }

        string responseText = request.downloadHandler.text;
        Debug.Log("Received response: " + responseText);
        return JsonConvert.DeserializeObject<TResponse>(responseText);
    }


}