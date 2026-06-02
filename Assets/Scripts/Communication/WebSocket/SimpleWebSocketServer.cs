using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 轻量WebSocket服务端实现（基于HttpListener），用于Unity本地监听
/// </summary>
public class WebSocketServer
{
    private readonly string _prefix;
    private HttpListener _listener;
    private CancellationTokenSource _cts;

    public event Action<WebSocketConnection> OnConnect;

    public WebSocketServer(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new ArgumentException("prefix 不能为空，示例: http://localhost:9001/ws/");
        _prefix = prefix.EndsWith("/") ? prefix : prefix + "/";
    }

    public Task Listen()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(_prefix);
        _listener.Start();

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => AcceptLoop(_cts.Token));

        return Task.CompletedTask;
    }

    private async Task AcceptLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            HttpListenerContext context = null;
            try
            {
                context = await _listener.GetContextAsync();
                if (!context.Request.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    continue;
                }

                var wsContext = await context.AcceptWebSocketAsync(subProtocol: null);
                var connection = new WebSocketConnection(wsContext.WebSocket);
                OnConnect?.Invoke(connection);
                connection.StartReceiveLoop();
            }
            catch (HttpListenerException)
            {
                // listener closed
                break;
            }
            catch (Exception ex)
            {
                if (context != null && context.Response.OutputStream.CanWrite)
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                Debug.LogError($"[WebSocketServer] 接收连接失败: {ex.Message}");
            }
        }
    }

    public void Close()
    {
        try
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener?.Close();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WebSocketServer] 关闭时发生异常: {ex.Message}");
        }
    }
}

public class WebSocketConnection
{
    private readonly WebSocket _socket;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    public event Action<byte[]> OnMessage;
    public event Action OnDisconnect;

    public WebSocketConnection(WebSocket socket)
    {
        _socket = socket;
    }

    public void StartReceiveLoop()
    {
        _ = Task.Run(ReceiveLoop);
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[4096];

        try
        {
            while (_socket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
            {
                using (var ms = new MemoryStream())
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            OnDisconnect?.Invoke();
                            return;
                        }

                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    OnMessage?.Invoke(ms.ToArray());
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WebSocketConnection] 接收循环异常: {ex.Message}");
        }
        finally
        {
            OnDisconnect?.Invoke();
        }
    }

    public Task SendAsync(byte[] data, bool isText = true)
    {
        if (_socket.State != WebSocketState.Open) return Task.CompletedTask;

        return _socket.SendAsync(
            new ArraySegment<byte>(data),
            isText ? WebSocketMessageType.Text : WebSocketMessageType.Binary,
            endOfMessage: true,
            cancellationToken: CancellationToken.None);
    }

    public void Send(byte[] data, bool isText = true)
    {
        _ = SendAsync(data, isText);
    }

    public void Close()
    {
        try
        {
            _cts.Cancel();
            if (_socket.State == WebSocketState.Open)
            {
                _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None).Wait(100);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WebSocketConnection] 关闭异常: {ex.Message}");
        }
        finally
        {
            OnDisconnect?.Invoke();
        }
    }
}

