using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// 通用 UDP 通信基座（被动监听服务端模式），所有回包均自动发给最近收到访问的客户端
public class UDPBaseCommunicator : MonoBehaviour
{
    public int localPort = 9001;      // 只需配置本地监听端口
    public bool autoStart = true;
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool running = false;
    private IPEndPoint lastClientEndPoint = null; // 记录最后一个客户端

    // 回调事件：收到数据(byte[] buffer, string str)
    public Action<byte[], string> OnDataReceived;

    // 初始化和启动监听
    public void StartCommunication()
    {
        if (running) return;
        udpClient = new UdpClient(localPort);
        running = true;
        receiveThread = new Thread(ListenLoop) {IsBackground = true};
        receiveThread.Start();
        Debug.Log($"[UDPBase] UDP监听启动->{localPort}");
    }

    // 停止通信
    public void StopCommunication()
    {
        running = false;
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Join();
        udpClient?.Close();
        Debug.Log("[UDPBase] UDP关闭");
    }

    // 主动回包给最近的客户端
    public void SendBytes(byte[] data)
    {
        if (udpClient == null || lastClientEndPoint == null) return;
        udpClient.Send(data, data.Length, lastClientEndPoint);
    }
    // 发送JSON字符串或文本命令
    public void SendString(string str)
    {
        var data = Encoding.UTF8.GetBytes(str);
        SendBytes(data);
    }
    // 发送对象（自动序列化成JSON）
    public void SendObject<T>(T obj)
    {
        string json = JsonUtility.ToJson(obj);
        SendString(json);
    }

    // 监听线程循环 - 被动接收所有客户端的包
    private void ListenLoop()
    {
        IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, localPort);
        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref endpoint);
                lastClientEndPoint = new IPEndPoint(endpoint.Address, endpoint.Port); // 只保存最后一个源
                string str = Encoding.UTF8.GetString(data);
                OnDataReceived?.Invoke(data, str);
            }
            catch (Exception ex)
            {
                if (running)
                    Debug.LogError("[UDPBase] UDP接收异常:" + ex.Message);
            }
        }
    }
    // 自动启动
    void Start() { if (autoStart) StartCommunication(); }
    void OnDestroy() { StopCommunication(); }
}
