using UnityEngine;
using System;
using Mujoco;

/// <summary>
/// H1 UDP通信控制器（服务端风格，无需配置远端）
/// Python客户端填入Unity监听端口即可，所有返回由基类自动发回上次访问源。
/// </summary>
public class UDP_H1Controller : UDPBaseCommunicator
{
    // 关节控制属性（与H1ControlCommandMsg字段一致）
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
        OnDataReceived += HandleReceivedData;
    }

    // 解析收到的数据-自动赋值控制命令给各个关节
    void HandleReceivedData(byte[] buf, string json)
    {
        try
        {
            H1ControlCommand cmd = JsonUtility.FromJson<H1ControlCommand>(json);
            if (cmd != null)
            {
                if (right_shoulder_pitch) right_shoulder_pitch.Control = cmd.right_shoulder_pitch;
                if (right_shoulder_roll) right_shoulder_roll.Control = cmd.right_shoulder_roll;
                if (right_shoulder_yaw) right_shoulder_yaw.Control = cmd.right_shoulder_yaw;
                if (right_elbow) right_elbow.Control = cmd.right_elbow;
                // TODO: 其余关节可以按需补全如上
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[UDP_H1Controller] 解析控制命令失败:" + ex.Message);
        }
    }

    // 对外接口：发送当前状态（如通过定时器发给Python端、或回包等）
    public void SendCurrentState()
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
        SendObject(state);
    }

    // 可在Update中周期性回报状态给Python端
    public float reportInterval = 0.05f;
    private float elapsed = 0f;
    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= reportInterval)
        {
            SendCurrentState();
            elapsed = 0f;
        }
    }
}
