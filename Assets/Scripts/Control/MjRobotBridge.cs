using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Mujoco;

/// <summary>
/// UDP bridge between Python policy controller and Unity MuJoCo simulation.
/// Receives joint torque commands from Python, applies them directly to actuators,
/// and sends back the full robot state (pelvis pose/IMU + joint q/dq).
///
/// Attach to an empty GameObject in the scene. Assign pelvisBody in Inspector.
/// </summary>
public class MjRobotBridge : MonoBehaviour
{
    [Header("Network")]
    public int receivePort = 9500;
    public int sendPort = 9501;
    public string pythonHost = "127.0.0.1";

    [Header("Robot References")]
    public MjBody pelvisBody;

    [Header("Actuator Order (must match Python side, 27 entries)")]
    public string[] actuatorOrder = new string[]
    {
        "left_hip_pitch_joint",
        "left_hip_roll_joint",
        "left_hip_yaw_joint",
        "left_knee_joint",
        "left_ankle_pitch_joint",
        "left_ankle_roll_joint",
        "right_hip_pitch_joint",
        "right_hip_roll_joint",
        "right_hip_yaw_joint",
        "right_knee_joint",
        "right_ankle_pitch_joint",
        "right_ankle_roll_joint",
        "waist_yaw_joint",
        "left_shoulder_pitch_joint",
        "left_shoulder_roll_joint",
        "left_shoulder_yaw_joint",
        "left_elbow_joint",
        "left_wrist_roll_joint",
        "left_wrist_pitch_joint",
        "left_wrist_yaw_joint",
        "right_shoulder_pitch_joint",
        "right_shoulder_roll_joint",
        "right_shoulder_yaw_joint",
        "right_elbow_joint",
        "right_wrist_roll_joint",
        "right_wrist_pitch_joint",
        "right_wrist_yaw_joint",
    };

    private const int NumJoints = 27;
    private const uint CmdMagic = 0x4731434D;
    private const uint StateMagic = 0x5331434D;

    // Cmd packet: magic(4) + seq(4) + sim_time(8) + joint_cmd(108) + kp(108) + kd(108) + base_cmd(12) = 352
    private const int CmdPacketSize = 352;
    // State packet: magic(4) + seq(4) + unity_time(8) + pos(12) + quat(16) + ang_vel(12) + lin_acc(12) + q(108) + dq(108) = 284
    private const int StatePacketSize = 284;

    private MjActuator[] _actuators;
    private MjHingeJoint[] _joints;
    private int[] _qposAddr;
    private int[] _dofAddr;
    private MjFreeJoint _freeJoint;

    private UdpClient _rxClient;
    private UdpClient _txClient;
    private IPEndPoint _pythonEndpoint;

    private volatile byte[] _latestCmdBytes;
    private float[] _targetQ = new float[NumJoints];
    private float[] _kp = new float[NumJoints];
    private float[] _kd = new float[NumJoints];
    private float[] _baseCmd = new float[3];
    private bool _hasValidCmd = false;

    private const float TorqueClamp = 200f;

    private uint _stateSeq = 0;
    private Vector3 _prevLinVel;
    private bool _hasPrevVel = false;

    private Thread _receiveThread;
    private volatile bool _running = true;

    void Awake()
    {
        Debug.Assert(Mathf.Approximately(Time.fixedDeltaTime, 0.01f),
            $"Time.fixedDeltaTime must be 0.01, got {Time.fixedDeltaTime}");

        CacheActuatorsAndJoints();
        CacheFreeJoint();
        SetupNetwork();

        // Register MuJoCo step callbacks
        MjScene.Instance.preUpdateEvent += OnPreMjStep;
        MjScene.Instance.postUpdateEvent += OnPostMjStep;

        Debug.Log($"MjRobotBridge ready, {_actuators.Length} actuators cached. " +
                  $"Listening on UDP :{receivePort}, sending to {pythonHost}:{sendPort}");
    }

    void OnDestroy()
    {
        _running = false;
        if (MjScene.Instance != null)
        {
            MjScene.Instance.preUpdateEvent -= OnPreMjStep;
            MjScene.Instance.postUpdateEvent -= OnPostMjStep;
        }
        _rxClient?.Close();
        _txClient?.Close();
        _receiveThread?.Join(500);
    }

    private void CacheActuatorsAndJoints()
    {
        var allActuators = FindObjectsOfType<MjActuator>();
        var lookup = new System.Collections.Generic.Dictionary<string, MjActuator>();
        foreach (var act in allActuators)
            lookup[act.name] = act;

        _actuators = new MjActuator[NumJoints];
        _joints = new MjHingeJoint[NumJoints];

        for (int i = 0; i < NumJoints; i++)
        {
            string name = actuatorOrder[i];
            if (!lookup.TryGetValue(name, out var actuator))
            {
                Debug.LogError($"MjRobotBridge: actuator '{name}' not found! Disabling.");
                enabled = false;
                return;
            }
            _actuators[i] = actuator;

            var joint = actuator.GetComponentInParent<MjHingeJoint>();
            if (joint == null)
            {
                // Try to find joint by matching name on the same GameObject or parent hierarchy
                var joints = FindObjectsOfType<MjHingeJoint>();
                foreach (var j in joints)
                {
                    if (j.name == name)
                    {
                        joint = j;
                        break;
                    }
                }
            }
            if (joint == null)
            {
                Debug.LogError($"MjRobotBridge: joint for actuator '{name}' not found! Disabling.");
                enabled = false;
                return;
            }
            _joints[i] = joint;
        }
    }

    private void CacheFreeJoint()
    {
        if (pelvisBody != null)
        {
            _freeJoint = pelvisBody.GetComponentInChildren<MjFreeJoint>();
        }
        if (_freeJoint == null)
        {
            _freeJoint = FindObjectOfType<MjFreeJoint>();
        }
        if (_freeJoint == null)
        {
            Debug.LogError("MjRobotBridge: no MjFreeJoint found!");
            enabled = false;
        }
    }

    private void SetupNetwork()
    {
        _pythonEndpoint = new IPEndPoint(IPAddress.Parse(pythonHost), sendPort);

        _txClient = new UdpClient();

        _rxClient = new UdpClient(receivePort);
        _rxClient.Client.ReceiveTimeout = 1000;

        _receiveThread = new Thread(ReceiveLoop)
        {
            IsBackground = true,
            Name = "MjBridge_UDP_Rx"
        };
        _receiveThread.Start();
    }

    private void ReceiveLoop()
    {
        var remoteEP = new IPEndPoint(IPAddress.Any, 0);
        while (_running)
        {
            try
            {
                byte[] data = _rxClient.Receive(ref remoteEP);
                if (data.Length >= CmdPacketSize)
                {
                    _latestCmdBytes = data;
                }
            }
            catch (SocketException)
            {
                // Timeout or closed, continue
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private unsafe void CacheJointAddressesIfNeeded()
    {
        if (_qposAddr != null) return;
        _qposAddr = new int[NumJoints];
        _dofAddr = new int[NumJoints];
        for (int i = 0; i < NumJoints; i++)
        {
            _qposAddr[i] = _joints[i].QposAddress;
            _dofAddr[i] = _joints[i].DofAddress;
        }
        Debug.Log($"MjRobotBridge: joint addresses cached. " +
                  $"freeJoint qpos@{_freeJoint.QposAddress} dof@{_freeJoint.DofAddress}, " +
                  $"first joint qpos@{_qposAddr[0]} dof@{_dofAddr[0]}");
    }

    private unsafe void OnPreMjStep(object sender, MjStepArgs e)
    {
        CacheJointAddressesIfNeeded();

        // Parse latest command packet
        byte[] cmdBytes = _latestCmdBytes;
        if (cmdBytes != null && cmdBytes.Length >= CmdPacketSize)
        {
            int offset = 0;
            uint magic = BitConverter.ToUInt32(cmdBytes, offset); offset += 4;
            if (magic == CmdMagic)
            {
                offset += 4; // skip seq
                offset += 8; // skip sim_time

                bool allFinite = true;
                float[] tmpTarget = new float[NumJoints];
                float[] tmpKp = new float[NumJoints];
                float[] tmpKd = new float[NumJoints];
                for (int i = 0; i < NumJoints; i++)
                {
                    tmpTarget[i] = BitConverter.ToSingle(cmdBytes, offset); offset += 4;
                    if (!float.IsFinite(tmpTarget[i])) allFinite = false;
                }
                for (int i = 0; i < NumJoints; i++)
                {
                    tmpKp[i] = BitConverter.ToSingle(cmdBytes, offset); offset += 4;
                    if (!float.IsFinite(tmpKp[i])) allFinite = false;
                }
                for (int i = 0; i < NumJoints; i++)
                {
                    tmpKd[i] = BitConverter.ToSingle(cmdBytes, offset); offset += 4;
                    if (!float.IsFinite(tmpKd[i])) allFinite = false;
                }
                for (int i = 0; i < 3; i++)
                {
                    _baseCmd[i] = BitConverter.ToSingle(cmdBytes, offset); offset += 4;
                }

                if (allFinite)
                {
                    Array.Copy(tmpTarget, _targetQ, NumJoints);
                    Array.Copy(tmpKp, _kp, NumJoints);
                    Array.Copy(tmpKd, _kd, NumJoints);
                    _hasValidCmd = true;
                }
                else
                {
                    Debug.LogWarning("MjRobotBridge: dropping packet with non-finite values");
                }
            }
        }

        // Before first valid command, leave ctrl at zero
        if (!_hasValidCmd)
        {
            for (int i = 0; i < NumJoints; i++)
                _actuators[i].Control = 0f;
            return;
        }

        // Apply torque command directly.
        for (int i = 0; i < NumJoints; i++)
        {
            float torque = _targetQ[i];
            if (!float.IsFinite(torque))
            {
                torque = 0f;
            }
            else
            {
                torque = Mathf.Clamp(torque, -TorqueClamp, TorqueClamp);
            }
            _actuators[i].Control = torque;
        }
    }

    private unsafe void OnPostMjStep(object sender, MjStepArgs e)
    {
        CacheJointAddressesIfNeeded();

        byte[] packet = new byte[StatePacketSize];
        int offset = 0;

        // Magic
        WriteUInt32(packet, ref offset, StateMagic);
        // Seq
        WriteUInt32(packet, ref offset, _stateSeq++);
        // Unity time
        WriteFloat64(packet, ref offset, Time.fixedTime);

        // Pelvis position from qpos[0..2] (MuJoCo frame, free joint)
        int qposAddr = _freeJoint.QposAddress;
        var data = e.data;
        float px = (float)data->qpos[qposAddr + 0];
        float py = (float)data->qpos[qposAddr + 1];
        float pz = (float)data->qpos[qposAddr + 2];
        WriteFloat32(packet, ref offset, px);
        WriteFloat32(packet, ref offset, py);
        WriteFloat32(packet, ref offset, pz);

        // Pelvis quaternion [w,x,y,z] from qpos[3..6]
        float qw = (float)data->qpos[qposAddr + 3];
        float qx = (float)data->qpos[qposAddr + 4];
        float qy = (float)data->qpos[qposAddr + 5];
        float qz = (float)data->qpos[qposAddr + 6];
        WriteFloat32(packet, ref offset, qw);
        WriteFloat32(packet, ref offset, qx);
        WriteFloat32(packet, ref offset, qy);
        WriteFloat32(packet, ref offset, qz);

        // Angular velocity from qvel[3..5] (body frame)
        int dofAddr = _freeJoint.DofAddress;
        float wx = (float)data->qvel[dofAddr + 3];
        float wy = (float)data->qvel[dofAddr + 4];
        float wz = (float)data->qvel[dofAddr + 5];
        WriteFloat32(packet, ref offset, wx);
        WriteFloat32(packet, ref offset, wy);
        WriteFloat32(packet, ref offset, wz);

        // Linear acceleration (finite difference of linear velocity - gravity in body frame)
        float vx = (float)data->qvel[dofAddr + 0];
        float vy = (float)data->qvel[dofAddr + 1];
        float vz = (float)data->qvel[dofAddr + 2];
        Vector3 linVel = new Vector3(vx, vy, vz);

        if (_hasPrevVel)
        {
            float dt = Time.fixedDeltaTime;
            Vector3 acc = (linVel - _prevLinVel) / dt;

            // Rotate gravity [0,0,-9.81] into body frame using inverse of pelvis quaternion
            Quaternion bodyQuat = new Quaternion(qx, qy, qz, qw); // Unity convention [x,y,z,w]
            Vector3 gravWorld = new Vector3(0f, 0f, -9.81f);
            Vector3 gravBody = Quaternion.Inverse(bodyQuat) * gravWorld;
            acc -= gravBody;

            WriteFloat32(packet, ref offset, acc.x);
            WriteFloat32(packet, ref offset, acc.y);
            WriteFloat32(packet, ref offset, acc.z);
        }
        else
        {
            WriteFloat32(packet, ref offset, 0f);
            WriteFloat32(packet, ref offset, 0f);
            WriteFloat32(packet, ref offset, 0f);
        }
        _prevLinVel = linVel;
        _hasPrevVel = true;

        // Joint positions (rad) — read raw qpos
        for (int i = 0; i < NumJoints; i++)
        {
            float q = (float)data->qpos[_qposAddr[i]];
            WriteFloat32(packet, ref offset, q);
        }

        // Joint velocities (rad/s) — read raw qvel
        for (int i = 0; i < NumJoints; i++)
        {
            float dq = (float)data->qvel[_dofAddr[i]];
            WriteFloat32(packet, ref offset, dq);
        }

        // Send
        try
        {
            _txClient.Send(packet, packet.Length, _pythonEndpoint);
        }
        catch (SocketException ex)
        {
            Debug.LogWarning($"MjRobotBridge: send failed: {ex.Message}");
        }
    }

    // --- Binary write helpers (little-endian) ---

    private static void WriteUInt32(byte[] buf, ref int offset, uint value)
    {
        buf[offset + 0] = (byte)(value & 0xFF);
        buf[offset + 1] = (byte)((value >> 8) & 0xFF);
        buf[offset + 2] = (byte)((value >> 16) & 0xFF);
        buf[offset + 3] = (byte)((value >> 24) & 0xFF);
        offset += 4;
    }

    private static void WriteFloat32(byte[] buf, ref int offset, float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, buf, offset, 4);
        offset += 4;
    }

    private static void WriteFloat64(byte[] buf, ref int offset, double value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, buf, offset, 8);
        offset += 8;
    }
}
