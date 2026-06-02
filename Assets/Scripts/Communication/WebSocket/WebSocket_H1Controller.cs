using UnityEngine;
using System;
using Mujoco;
using System.Text;

/// <summary>
/// H1机器人WebSocket通信控制器，服务端监听模式，支持多客户端
/// </summary>
public class WebSocket_H1Controller : WebSocketBaseCommunicator
{
#if UNITY_STANDALONE || UNITY_EDITOR
    // 与H1ControlCommandMsg结构一致
    public MjActuator left_hip_yaw;
    public MjActuator left_hip_roll;
    public MjActuator left_hip_pitch;
    public MjActuator left_knee;
    public MjActuator left_ankle;
    public MjActuator right_hip_yaw;
    public MjActuator right_hip_roll;
    public MjActuator right_hip_pitch;
    public MjActuator right_knee;
    public MjActuator right_ankle;
    public MjActuator torso;
    public MjActuator left_shoulder_pitch;
    public MjActuator left_shoulder_roll;
    public MjActuator left_shoulder_yaw;
    public MjActuator left_elbow;
    public MjActuator right_shoulder_pitch;
    public MjActuator right_shoulder_roll;
    public MjActuator right_shoulder_yaw;
    public MjActuator right_elbow;

    [Serializable]
    public class H1ControlCommand
    {
        public float left_hip_yaw; public float left_hip_roll; public float left_hip_pitch;
        public float left_knee; public float left_ankle;
        public float right_hip_yaw; public float right_hip_roll; public float right_hip_pitch;
        public float right_knee; public float right_ankle;
        public float torso;
        public float left_shoulder_pitch; public float left_shoulder_roll; public float left_shoulder_yaw; public float left_elbow;
        public float right_shoulder_pitch; public float right_shoulder_roll; public float right_shoulder_yaw; public float right_elbow;
    }

    void Awake()
    {
        OnMessage += OnWsMessage;
    }

    void OnWsMessage(WebSocketConnection client, byte[] data)
    {
        try
        {
            string json = Encoding.UTF8.GetString(data);
            H1ControlCommand cmd = JsonUtility.FromJson<H1ControlCommand>(json);
            if (cmd != null)
            {
                if (right_shoulder_pitch) right_shoulder_pitch.Control = cmd.right_shoulder_pitch;
                if (right_shoulder_roll) right_shoulder_roll.Control = cmd.right_shoulder_roll;
                if (right_shoulder_yaw) right_shoulder_yaw.Control = cmd.right_shoulder_yaw;
                if (right_elbow) right_elbow.Control = cmd.right_elbow;
                // TODO: 其余关节可类似赋值
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[WebSocket_H1] 解析或赋值失败: " + ex.Message);
        }
    }

    // 定时同步状态发给所有client（按需可调整为回包机制）
    public float reportInterval = 0.05f;
    private float elapsed = 0f;
    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= reportInterval)
        {
            SendCurrentStateToAll();
            elapsed = 0f;
        }
    }

    public void SendCurrentStateToAll()
    {
        H1ControlCommand state = new H1ControlCommand
        {
            left_hip_yaw = left_hip_yaw?.Control ?? 0,
            left_hip_roll = left_hip_roll?.Control ?? 0,
            left_hip_pitch = left_hip_pitch?.Control ?? 0,
            left_knee = left_knee?.Control ?? 0,
            left_ankle = left_ankle?.Control ?? 0,
            right_hip_yaw = right_hip_yaw?.Control ?? 0,
            right_hip_roll = right_hip_roll?.Control ?? 0,
            right_hip_pitch = right_hip_pitch?.Control ?? 0,
            right_knee = right_knee?.Control ?? 0,
            right_ankle = right_ankle?.Control ?? 0,
            torso = torso?.Control ?? 0,
            left_shoulder_pitch = left_shoulder_pitch?.Control ?? 0,
            left_shoulder_roll = left_shoulder_roll?.Control ?? 0,
            left_shoulder_yaw = left_shoulder_yaw?.Control ?? 0,
            left_elbow = left_elbow?.Control ?? 0,
            right_shoulder_pitch = right_shoulder_pitch?.Control ?? 0,
            right_shoulder_roll = right_shoulder_roll?.Control ?? 0,
            right_shoulder_yaw = right_shoulder_yaw?.Control ?? 0,
            right_elbow = right_elbow?.Control ?? 0
        };
        string stateJson = JsonUtility.ToJson(state);
        SendToAll(stateJson);
    }
#endif
}
