using System;
using System.Text;
using System.Net.Http;
using UnityEngine;
using System.Threading.Tasks;

public class RestClient : MonoBehaviour
{
    [Serializable]
    public class MyMessage
    {
        public string message;
        public string error;
        public string id;
    }

    private const string serverURL = "http://localhost:8000/hello";
    private static readonly HttpClient client = new HttpClient();

    async void Start()
    {
        await PostMessageAsync();
    }

    private async Task PostMessageAsync()
    {
        MyMessage message = new MyMessage
        {
            message = "Hello server!",
            error = "",
            id = Guid.NewGuid().ToString()
        };

        StringContent reqBody = new StringContent(
            JsonUtility.ToJson(message),
            Encoding.UTF8,
            "application/json"
        );

        try
        {
            HttpResponseMessage response = await client.PostAsync(serverURL, reqBody);
            string result = await response.Content.ReadAsStringAsync();
            
            Debug.Log($"Status Code: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                MyMessage resJson = JsonUtility.FromJson<MyMessage>(result);
                Debug.Log(resJson.message);
                Debug.Log(resJson.error);
                Debug.Log(resJson.id);
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
    }
}
