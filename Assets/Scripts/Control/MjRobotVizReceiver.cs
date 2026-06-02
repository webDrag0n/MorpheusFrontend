using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Mujoco;

/// <summary>
/// Receives robot pose data from MuJoCo simulation via UDP and visualizes it in Unity.
/// This script does NOT run its own physics - it directly writes joint positions (qpos)
/// into the Unity MuJoCo Plugin's data, letting the plugin handle Transform synchronization.
///
/// Attach to an empty GameObject in the scene. Assign pelvisBody in Inspector.
/// The Python MuJoCo simulation runs separately and streams pose data to this receiver.
///
/// IMPORTANT: Disable MuJoCo stepping in the scene (set MjScene timestep to 0 or pause),
/// or this script will fight with the local simulation. This script is for visualization only.
/// </summary>
public class MjRobotVizReceiver : MonoBehaviour
{
    [Header("Network")]
    public int receivePort = 9600;

    [Header("Robot References")]
    public MjBody pelvisBody;

    [Header("Joint Smoothing")]
    [Tooltip("Smoothing speed for joints (higher = faster tracking, lower = smoother)")]
    [Range(1f, 60f)]
    public float jointLerpSpeed = 20f;

    [Tooltip("Smoothing speed for pelvis position")]
    [Range(1f, 60f)]
    public float pelvisLerpSpeed = 15f;

    [Tooltip("Smoothing speed for pelvis rotation")]
    [Range(1f, 60f)]
    public float pelvisRotLerpSpeed = 15f;

    [Header("Joint Order (must match Python side, 27 entries)")]
    public string[] jointOrder = new string[]
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
    private const uint VizMagic = 0x5A495647; // "GVIZ" LE

    // VizPacket: magic(4) + seq(4) + sim_time(8) + pelvis_pos(12) + pelvis_quat(16) + joint_q(108) = 152
    private const int VizPacketSize = 152;

    private MjHingeJoint[] _joints;
    private MjFreeJoint _freeJoint;
    private int[] _qposAddr;
    private int _freeJointQposAddr;

    private float[] _currentJointAngles = new float[NumJoints];
    private float[] _targetJointAngles = new float[NumJoints];

    private Vector3 _targetPelvisPos;
    private Quaternion _targetPelvisRot;
    private Vector3 _currentPelvisPos;
    private Quaternion _currentPelvisRot;

    private UdpClient _rxClient;
    private Thread _receiveThread;
    private volatile bool _running = true;
    private volatile byte[] _latestVizBytes;

    private uint _lastSeq = 0;
    private float _lastReceiveTime = 0f;
    private int _packetsReceived = 0;
    private bool _addressesCached = false;

    void Awake()
    {
        CacheJoints();
        CacheFreeJoint();
        SetupNetwork();

        // Hook into MuJoCo step to write qpos before physics
        MjScene.Instance.preUpdateEvent += OnPreMjStep;

        Debug.Log($"MjRobotVizReceiver ready, {NumJoints} joints cached. " +
                  $"Listening on UDP :{receivePort}");
    }

    void OnDestroy()
    {
        _running = false;
        if (MjScene.Instance != null)
        {
            MjScene.Instance.preUpdateEvent -= OnPreMjStep;
        }
        _rxClient?.Close();
        _receiveThread?.Join(500);
    }

    private void CacheJoints()
    {
        var allJoints = FindObjectsOfType<MjHingeJoint>();
        var lookup = new System.Collections.Generic.Dictionary<string, MjHingeJoint>();
        foreach (var j in allJoints)
            lookup[j.name] = j;

        _joints = new MjHingeJoint[NumJoints];

        for (int i = 0; i < NumJoints; i++)
        {
            string name = jointOrder[i];
            if (!lookup.TryGetValue(name, out var joint))
            {
                Debug.LogError($"MjRobotVizReceiver: MjHingeJoint '{name}' not found! Disabling.");
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
            Debug.LogError("MjRobotVizReceiver: no MjFreeJoint found!");
            enabled = false;
        }
    }

    private void CacheAddresses()
    {
        if (_addressesCached) return;

        _qposAddr = new int[NumJoints];
        for (int i = 0; i < NumJoints; i++)
        {
            _qposAddr[i] = _joints[i].QposAddress;
        }
        _freeJointQposAddr = _freeJoint.QposAddress;
        _addressesCached = true;

        Debug.Log($"MjRobotVizReceiver: addresses cached. " +
                  $"freeJoint qpos@{_freeJointQposAddr}, first joint qpos@{_qposAddr[0]}");
    }

    private void SetupNetwork()
    {
        _rxClient = new UdpClient(receivePort);
        _rxClient.Client.ReceiveTimeout = 1000;

        _receiveThread = new Thread(ReceiveLoop)
        {
            IsBackground = true,
            Name = "MjViz_UDP_Rx"
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
                if (data.Length >= VizPacketSize)
                {
                    _latestVizBytes = data;
                }
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    void Update()
    {
        // Parse latest visualization packet
        byte[] vizBytes = _latestVizBytes;
        if (vizBytes != null && vizBytes.Length >= VizPacketSize)
        {
            int offset = 0;
            uint magic = BitConverter.ToUInt32(vizBytes, offset); offset += 4;

            if (magic == VizMagic)
            {
                uint seq = BitConverter.ToUInt32(vizBytes, offset); offset += 4;

                if (seq > _lastSeq)
                {
                    _lastSeq = seq;
                    _packetsReceived++;
                    _lastReceiveTime = Time.time;

                    offset += 8; // skip sim_time

                    // Pelvis position (MuJoCo frame: x forward, y left, z up)
                    float px = BitConverter.ToSingle(vizBytes, offset); offset += 4;
                    float py = BitConverter.ToSingle(vizBytes, offset); offset += 4;
                    float pz = BitConverter.ToSingle(vizBytes, offset); offset += 4;
                    _targetPelvisPos = new Vector3(px, py, pz);

                    // Pelvis quaternion [w,x,y,z] MuJoCo convention
                    float qw = BitConverter.ToSingle(vizBytes, offset); offset += 4;
                    float qx = BitConverter.ToSingle(vizBytes, offset); offset += 4;
                    float qy = BitConverter.ToSingle(vizBytes, offset); offset += 4;
                    float qz = BitConverter.ToSingle(vizBytes, offset); offset += 4;
                    _targetPelvisRot = new Quaternion(qx, qy, qz, qw);

                    // Joint positions (rad)
                    bool allFinite = true;
                    for (int i = 0; i < NumJoints; i++)
                    {
                        float q = BitConverter.ToSingle(vizBytes, offset); offset += 4;
                        if (!float.IsFinite(q))
                        {
                            allFinite = false;
                            break;
                        }
                        _targetJointAngles[i] = q;
                    }

                    if (!allFinite)
                    {
                        Debug.LogWarning($"MjRobotVizReceiver: dropping packet {seq} with non-finite values");
                    }
                }
            }
        }

        // Smooth interpolation
        float dt = Time.deltaTime;
        float jointAlpha = 1f - Mathf.Exp(-jointLerpSpeed * dt);
        float posAlpha = 1f - Mathf.Exp(-pelvisLerpSpeed * dt);
        float rotAlpha = 1f - Mathf.Exp(-pelvisRotLerpSpeed * dt);

        _currentPelvisPos = Vector3.Lerp(_currentPelvisPos, _targetPelvisPos, posAlpha);
        _currentPelvisRot = Quaternion.Slerp(_currentPelvisRot, _targetPelvisRot, rotAlpha);

        for (int i = 0; i < NumJoints; i++)
        {
            _currentJointAngles[i] = Mathf.Lerp(_currentJointAngles[i], _targetJointAngles[i], jointAlpha);
        }
    }

    private unsafe void OnPreMjStep(object sender, MjStepArgs e)
    {
        CacheAddresses();

        var data = e.data;

        // Write pelvis pose into free joint qpos [x, y, z, qw, qx, qy, qz]
        data->qpos[_freeJointQposAddr + 0] = _currentPelvisPos.x;
        data->qpos[_freeJointQposAddr + 1] = _currentPelvisPos.y;
        data->qpos[_freeJointQposAddr + 2] = _currentPelvisPos.z;
        data->qpos[_freeJointQposAddr + 3] = _currentPelvisRot.w;
        data->qpos[_freeJointQposAddr + 4] = _currentPelvisRot.x;
        data->qpos[_freeJointQposAddr + 5] = _currentPelvisRot.y;
        data->qpos[_freeJointQposAddr + 6] = _currentPelvisRot.z;

        // Write joint angles into qpos
        for (int i = 0; i < NumJoints; i++)
        {
            data->qpos[_qposAddr[i]] = _currentJointAngles[i];
        }

        // Zero out velocities so MuJoCo doesn't accumulate momentum
        int dofAddr = _freeJoint.DofAddress;
        for (int k = 0; k < 6; k++)
            data->qvel[dofAddr + k] = 0;

        for (int i = 0; i < NumJoints; i++)
            data->qvel[_joints[i].DofAddress] = 0;

        // Zero out all controls (no forces applied)
        var allActuators = FindObjectsOfType<MjActuator>();
        foreach (var act in allActuators)
            act.Control = 0f;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;

        float timeSinceLastPacket = Time.time - _lastReceiveTime;
        bool connected = timeSinceLastPacket < 1.0f;

        style.normal.textColor = connected ? Color.green : Color.red;
        string status = connected ? "CONNECTED" : "DISCONNECTED";

        GUI.Label(new Rect(10, 10, 400, 30), $"MuJoCo Viz: {status}", style);
        GUI.Label(new Rect(10, 30, 400, 30), $"Packets: {_packetsReceived} | Seq: {_lastSeq}", style);
        GUI.Label(new Rect(10, 50, 400, 30),
            $"Pelvis: ({_currentPelvisPos.x:F2}, {_currentPelvisPos.y:F2}, {_currentPelvisPos.z:F2})", style);
    }
}
