
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
//public static class ExtensionMethods
//{
//    public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
//    {
//        var tcs = new TaskCompletionSource<object>();
//        asyncOp.completed += obj => { tcs.SetResult(null); };
//        return ((Task)tcs.Task).GetAwaiter();
//    }
//}
public class DeepSeekIntegration : MonoBehaviour
{
    // ���ڴ洢�Ի���ʷ
    private List<Dictionary<string, string>> messages = new List<Dictionary<string, string>>();
    void Start()
    {
        // ��ʼ��ϵͳ��Ϣ
        messages.Add(new Dictionary<string, string> { { "role", "system" }, { "content", "You are a helpful assistant." } });
    }

    public void OnSendButtonClicked()
    {
        string userMessage = "Please design a 4m x 6m kitchen for me, suppose each item takes a 1x1 square, output the result as a 2d array, each element is the name of the item, allow empty element, please output the 2d array only, with no explainations";
        //if (string.IsNullOrEmpty(userMessage)) return;

        // �����û���Ϣ���Ի���ʷ
        messages.Add(new Dictionary<string, string> { { "role", "user" }, { "content", userMessage } });
        // ���� DeepSeek API
        StartCoroutine(CallDeepSeekAPI());
    }

    private IEnumerator CallDeepSeekAPI()
    {
        // ������������
        var requestData = new
        {
            model = "deepseek-chat",
            messages = messages,
            stream = false
        };

        string jsonData = JsonConvert.SerializeObject(requestData);
        Debug.Log(jsonData);

        // ���� UnityWebRequest
        UnityWebRequest request = new UnityWebRequest("https://api.deepseek.com/chat/completions", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + "sk-b9be58803d35454fb7102491b5c455ee");

        // ��������
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // ������Ӧ
            var response = JsonConvert.DeserializeObject<DeepSeekResponse>(request.downloadHandler.text);
            string botMessage = response.choices[0].message.content;

            // ��ʾ��Ӧ
            Debug.Log("\nAI: " + botMessage);

            // ���� AI ��Ϣ���Ի���ʷ
            messages.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", botMessage } });
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }

    [System.Serializable]
    public class DeepSeekResponse
    {
        public Choice[] choices;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }

    [System.Serializable]
    public class Message
    {
        public string content;
    }
}
