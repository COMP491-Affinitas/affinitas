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


public enum ServerDirectory
{
    init,
    load,
    npc
}

public class ServerConnection : MonoBehaviour
{
    public static ServerConnection Instance { get; private set; }

    const string serverURL = "https://affinitas.onrender.com";
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
    public async Task<BaseResponse> SendAndGetMessageFromServer<BaseRequest, BaseResponse>(BaseRequest message, string directoryPath, HttpMethod method = null)
    {
        
        if (method == null) {
            method = HttpMethod.Post;
        }
        
        var requestMessage = new HttpRequestMessage(method, serverURL + directoryPath);

        if (method == HttpMethod.Post) {
            string jsonString = JsonUtility.ToJson(message);
            requestMessage.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
        }

        // Set the header x-client-uuid
        if (!string.IsNullOrEmpty(GameManager.Instance.gameId))
        {
            requestMessage.Headers.Add("x-client-uuid", GameManager.Instance.gameId);
        }

        BaseResponse serverResponse = default;
        try
        {
            // Send request and wait for response
            var response = await client.SendAsync(requestMessage);
            string result = await response.Content.ReadAsStringAsync();
            Debug.Log($"Server raw response: {result}");
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

}