
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
    private List<Dictionary<string, string>> messages = new List<Dictionary<string, string>>();

    public struct ObjectPrefabs
    {
        public string name;
        public GameObject prefab;
    }
    
    public GameObject[] objectPrefabs;

    void Start()
    {
        messages.Add(new Dictionary<string, string> { { "role", "system" }, { "content", "You are a helpful assistant." } });
    }

    public void OnSendButtonClicked()
    {
        string userMessage = "Please design a 4m x 6m kitchen for me, suppose each item takes a 1x1 square, output the result as a 2d array, each element is the name of the item, allow empty element, please output the 2d array only, with no explainations, you can choose object component from the following list: [oven,fridge,table,chair,cabinet], you can put multiple table/chair/cabinet in the kitchen, please only output the 2d array, with no explainations, no other text, no markdown or json format, no spaces between elements, no need to output string quote symbols, no need to output the array parenthese '[' and ']', just output the 2d array content";
        //if (string.IsNullOrEmpty(userMessage)) return;

        messages.Add(new Dictionary<string, string> { { "role", "user" }, { "content", userMessage } });
        // DeepSeek API
        StartCoroutine(CallDeepSeekAPI());
    }

    private IEnumerator CallDeepSeekAPI()
    {
        // DeepSeek API
        var requestData = new
        {
            model = "deepseek-chat",
            messages = messages,
            stream = false
        };

        string jsonData = JsonConvert.SerializeObject(requestData);
        Debug.Log(jsonData);

        // UnityWebRequest
        UnityWebRequest request = new UnityWebRequest("https://api.deepseek.com/chat/completions", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + "sk-b9be58803d35454fb7102491b5c455ee");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonConvert.DeserializeObject<DeepSeekResponse>(request.downloadHandler.text);
            string botMessage = response.choices[0].message.content;

            // 显示响应
            Debug.Log("\nAI: " + botMessage);

            // 解析 botMessage 为 2d array
            string[] rows = botMessage.Split('\n');
            string[][] result = new string[rows.Length][];
            for (int i = 0; i < rows.Length; i++)
            {
                result[i] = rows[i].Split(',');
            }

            // clear all children
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // 根据 result 生成游戏对象
            for (int i = 0; i < result.Length; i++)
            {
                for (int j = 0; j < result[i].Length; j++)
                {
                    string item = result[i][j];
                    if (item != ""){
                        foreach (GameObject prefab in objectPrefabs)
                        {
                            Debug.Log(item);
                            if (prefab.name == item)
                            {
                                GameObject obj = Instantiate(prefab, transform.position, Quaternion.identity, parent: transform);
                                obj.transform.position = new Vector3(i*2, 0, j*2);
                            }else{
                                Debug.Log("not found: " + item);
                            }
                        }
                    }
                }
            }

            // AI 信息添加到历史记录
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
