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
public class UuidResponse : BaseRequest
{
    public string uuid;
}


[Serializable]
public class ClientResponse
{
    string role;     //"system" or "user"
    //int requestId;   //Message classification (sendingPlayerInput, gettingQuest, ...)
    string content;

    public ClientResponse(string role, string content)
    {
        this.role = role;
        //this.requestId = requestId;
        this.content = content;
    }
}

[Serializable]
public class ServerResponse
{
    public int npcId;
    public int affinitasChange;
    //public int requestId; //Message classification (sendingNpcResponse, sendingQuest, ???)
    public string npcResponse;
    public string summary;
    public string error;
    public string messageId;
}

//[Serializable]
//public class LoadGameRequest
//{
//    string gameId;

//    public LoadGameRequest(string gameId)
//    {
//        this.gameId = gameId;
//    }
//}

public enum ServerDirectory
{
    init,
    load,
    npc
}

public class ServerConnection : MonoBehaviour
{
    public static ServerConnection Instance { get; private set; }

    const string serverURL = "http://localhost:8000";
    static readonly HttpClient client = new HttpClient();

    public bool canSendMessage = true;

    public Dictionary<int, string> serverDirectoriesDict = new Dictionary<int, string>{
        { (int)ServerDirectory.load, "/game/load" },
        { (int)ServerDirectory.npc, "/npcs/" },
        { (int)ServerDirectory.init, "/auth/uuid"}
    };

    HttpResponseMessage response;

    private void Start()
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


    //async void Start()
    //{
    //    await LoadGame();
    //}

    //async void InitGame()
    //{
    //    LoadGameRequest message = new LoadGameRequest("");
    //    ServerResponse serverResponse = await SendLoadGameRequest(message, ServerDirectory.init);
    //    if (serverResponse != null)
    //    {
    //        GameManager.Instance.gameId = serverResponse.
    //    }

    //}

    //async void LoadGame()
    //{

    //    LoadGameRequest message = new LoadGameRequest("")

    //    await SendLoadGameRequest(message, ServerDirectory.load);
    //}


    // Send and Get Generic Response from Server
    public async Task<BaseResponse> SendAndGetMessageFromServer<BaseRequest, BaseResponse>(BaseRequest message, string directoryPath)
    {
        // Change to JSON
        StringContent reqBody = new StringContent(JsonUtility.ToJson(message), Encoding.UTF8, "application/json");

        BaseResponse serverResponse = default;
        try
        {
            // Send request and wait for response
            var response = await client.PostAsync(serverURL + directoryPath, reqBody);
            string result = await response.Content.ReadAsStringAsync();

            Debug.Log($"Status Code: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
                // Change back from JSON
                serverResponse = JsonUtility.FromJson<BaseResponse>(result);
            else
                Debug.LogError($"Request failed: {response.StatusCode} - {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception occurred: {ex.Message}");
        }

        return serverResponse;
    }


    //public async Task<ServerResponse> SendAndGetMessageFromServer(ClientResponse message, int directory)
    //{
    //    string completeServerURL = serverURL + serverDirectoriesDict[directory];

    //    // Change to JSON
    //    StringContent reqBody = new StringContent(
    //        JsonUtility.ToJson(message),
    //        Encoding.UTF8,
    //        "application/json"
    //    );

    //    ServerResponse serverResponse = null;

    //    try
    //    {
    //        // Send player input text and wait for message
    //        response = await client.PostAsync(completeServerURL, reqBody);
    //        string result = await response.Content.ReadAsStringAsync();

    //        Debug.Log($"Status Code: {response.StatusCode}");

    //        if (response.IsSuccessStatusCode)
    //            serverResponse = JsonUtility.FromJson<ServerResponse>(result);
    //        else
    //            Debug.LogError($"Request failed: {response.StatusCode} - {response.ReasonPhrase}");
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogError($"Exception occurred: {ex.Message}");
    //    }

    //    return serverResponse;

    //}
}
