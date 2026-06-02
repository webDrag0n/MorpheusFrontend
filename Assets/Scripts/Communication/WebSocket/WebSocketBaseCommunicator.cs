using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class WebSocketBaseCommunicator : MonoBehaviour
{
#if UNITY_STANDALONE || UNITY_EDITOR
    public int port = 9001;
    [Tooltip("HttpListener监听地址（需包含http://主机名），如http://localhost 或 http://+: 需系统授权")]
    public string listenAddress = "http://localhost";
    [Tooltip("WebSocket路径，必须以/开头，例如 /ws/ 或 /")]
    public string listenPath = "/ws/";

    private WebSocketServer server;
    private List<WebSocketConnection> clients = new List<WebSocketConnection>();

    // 消息事件：任意客户端发来消息时
    public Action<WebSocketConnection, byte[]> OnMessage;
    // 新客户端连接事件
    public Action<WebSocketConnection> OnClientConnect;
    // 客户端断开事件
    public Action<WebSocketConnection> OnClientDisconnect;

    private async void Start()
    {
        string prefix = ComposePrefix();
        server = new WebSocketServer(prefix);
        server.OnConnect += client => {
            clients.Add(client);
            OnClientConnect?.Invoke(client);
            client.OnMessage += data => OnMessage?.Invoke(client, data);
            client.OnDisconnect += () => {
                clients.Remove(client);
                OnClientDisconnect?.Invoke(client);
            };
        };
        await server.Listen();
        Debug.Log($"[WebSocketBase] 监听WS前缀: {prefix}");
    }

    public void Send(WebSocketConnection client, string msg)
    {
        var data = Encoding.UTF8.GetBytes(msg);
        client.Send(data);
    }
    public void Send(WebSocketConnection client, byte[] data)
    {
        client.Send(data);
    }
    public void SendToAll(string msg)
    {
        var data = Encoding.UTF8.GetBytes(msg);
        foreach (var c in clients) c.Send(data);
    }
    public void SendToAll(byte[] data)
    {
        foreach (var c in clients) c.Send(data);
    }

    private void OnDestroy()
    {
        if (server != null) server.Close();
    }

    private string ComposePrefix()
    {
        string prefix = listenAddress.EndsWith("/") ? listenAddress : listenAddress + "/";
        string normalizedPath = listenPath.StartsWith("/") ? listenPath.Substring(1) : listenPath;
        if (!string.IsNullOrEmpty(normalizedPath) && !normalizedPath.EndsWith("/"))
            normalizedPath += "/";
        return $"{prefix.TrimEnd('/')}{(string.IsNullOrEmpty(normalizedPath) ? ":" + port + "/" : $":{port}/{normalizedPath}")}";
    }
#else
    // 非支持平台提示
    void Start() { Debug.LogError("WebSocket not supported on this platform"); }
#endif
}
