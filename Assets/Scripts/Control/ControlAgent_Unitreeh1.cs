using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Mujoco;
using System;
using System.Linq;

[System.Serializable]
public class ObservationScales
{
    public float lin_vel = 2.0f;
    public float ang_vel = 0.25f;
    public float dof_pos = 1.0f;
    public float dof_vel = 0.05f;
    public float commands_scale = 2.0f;
}

[System.Serializable]
public class JointParams
{
    [Tooltip("关节默认位置 (弧度)")]
    public float defaultPosition = 0f;
    [Tooltip("关节位置下限 (弧度)")]
    public float positionLowerLimit = -Mathf.PI;
    [Tooltip("关节位置上限 (弧度)")]
    public float positionUpperLimit = Mathf.PI;
    [Tooltip("关节速度限制 (弧度/秒)")]
    public float velocityLimit = 10f;
    [Tooltip("关节扭矩限制 (N·m)")]
    public float torqueLimit = 100f;
    [Tooltip("执行器控制范围 (最小值, 最大值)")]
    public Vector2 controlRange = new Vector2(-1f, 1f);
    
    public JointParams() { }
    
    public JointParams(float defaultPos, float posLower, float posUpper, float velLimit, float torqueLimit)
    {
        this.defaultPosition = defaultPos;
        this.positionLowerLimit = posLower;
        this.positionUpperLimit = posUpper;
        this.velocityLimit = velLimit;
        this.torqueLimit = torqueLimit;
        this.controlRange = new Vector2(-1f, 1f);
    }
}

[System.Serializable]
public class RewardsConfig
{
    public float base_height_target = 0.89f;
    public float tracking_sigma = 0.25f;
    public float max_contact_force = 200f;
    public float soft_dof_vel_limit = 1.0f;
    public float soft_torque_limit = 1.0f;
    
    // 奖励权重
    public float lin_vel_z_weight = -2.0f;
    public float ang_vel_xy_weight = -0.05f;
    public float orientation_weight = -0.2f;
    public float base_height_weight = -0.0f;
    public float torques_weight = -0.00001f;
    public float dof_vel_weight = -0.0f;
    public float dof_acc_weight = -2.5e-7f;
    public float action_rate_weight = -0.01f;
    public float collision_weight = -1.0f;
    public float termination_weight = -0.0f;
    public float dof_pos_limits_weight = -10.0f;
    public float dof_vel_limits_weight = -1.0f;
    public float torque_limits_weight = -1.0f;
    public float tracking_lin_vel_weight = 1.0f;
    public float tracking_ang_vel_weight = 0.5f;
    public float feet_air_time_weight = 1.0f;
    public float stumble_weight = -0.1f;
    public float stand_still_weight = -0.0f;
    public float feet_contact_forces_weight = -0.01f;
}

/// <summary>
/// Unity ML-Agents 强化学习控制器 - Unitree H1人形机器人
/// 使用MuJoCo物理引擎进行仿真
/// </summary>
    public class ControlAgent_Unitreeh1 : Agent
    {
    [Header("配置")]
    public ObservationScales obs_scales = new ObservationScales();
    public RewardsConfig rewards_config = new RewardsConfig();
    
    [Header("动作空间配置")]
    [Tooltip("控制的关节数量（从第一个关节开始）")]
    public int controlledJointCount = 10;
    
    [Header("初始化选项")]
    [Tooltip("是否自动查找并设置关节组件")]
    public bool autoInitializeComponents = true;
    
    [Tooltip("是否从MjHingeJoint组件自动更新关节参数（位置限制和默认位置）")]
    public bool autoUpdateJointParamsFromHinge = true;
    
    [Tooltip("自动查找组件时是否使用宽松匹配（GameObject名称包含关节名即可，而不是完全相等）")]
    public bool useLooseMatching = false;
    
    [Tooltip("是否显示详细的调试日志")]
    public bool showDebugLogs = false;
    
    [Header("目标和控制")]
    public Vector3 commands = Vector3.zero; // [x_vel, y_vel, yaw_vel]
        public Transform head_self;
    public float force_multiplier = 1000;
    public float dt = 0.02f; // 固定时间步长
    
    [Header("重置参数")]
    [Tooltip("机器人重置时的初始高度（米）")]
    public float resetHeight = 0.0f;
    
    [Header("MuJoCo Body组件")]
    [Tooltip("机器人的根body组件，用于获取基础速度")]
    public MjBaseBody rootBody;

    [Header("执行器")]
        public MjActuator left_hip_yaw_actuator;
        public MjActuator left_hip_roll_actuator;
        public MjActuator left_hip_pitch_actuator;
        public MjActuator left_knee_actuator;
        public MjActuator left_ankle_actuator;
        public MjActuator right_hip_yaw_actuator;
        public MjActuator right_hip_roll_actuator;
        public MjActuator right_hip_pitch_actuator;
        public MjActuator right_knee_actuator;
        public MjActuator right_ankle_actuator;
        public MjActuator torso_actuator;
        public MjActuator left_shoulder_pitch_actuator;
        public MjActuator left_shoulder_roll_actuator;
        public MjActuator left_shoulder_yaw_actuator;
        public MjActuator left_elbow_actuator;
        public MjActuator right_shoulder_pitch_actuator;
        public MjActuator right_shoulder_roll_actuator;
        public MjActuator right_shoulder_yaw_actuator;
        public MjActuator right_elbow_actuator;

    [Header("关节状态")]
        public MjHingeJoint left_hip_yaw_joint;
        public MjHingeJoint left_hip_roll_joint;
        public MjHingeJoint left_hip_pitch_joint;
        public MjHingeJoint left_knee_joint;
        public MjHingeJoint left_ankle_joint;
        public MjHingeJoint right_hip_yaw_joint;
        public MjHingeJoint right_hip_roll_joint;
        public MjHingeJoint right_hip_pitch_joint;
        public MjHingeJoint right_knee_joint;
        public MjHingeJoint right_ankle_joint;
        public MjHingeJoint torso_joint;
        public MjHingeJoint left_shoulder_pitch_joint;
        public MjHingeJoint left_shoulder_roll_joint;
        public MjHingeJoint left_shoulder_yaw_joint;
        public MjHingeJoint left_elbow_joint;
        public MjHingeJoint right_shoulder_pitch_joint;
        public MjHingeJoint right_shoulder_roll_joint;
        public MjHingeJoint right_shoulder_yaw_joint;
        public MjHingeJoint right_elbow_joint;

    [Header("接触检测")]
    public Transform[] feet_transforms;
    public Transform[] penalised_contact_bodies;
    public float contact_threshold = 0.1f;

        [Header("关节参数配置")]
    [Space(5)]
    [Tooltip("展开查看各个关节的详细参数设置")]
    public JointParams left_hip_yaw_params = new JointParams(0f, -2.9f, 2.9f, 10f, 200f);
    public JointParams left_hip_roll_params = new JointParams(0f, -0.5f, 0.5f, 10f, 200f);
    public JointParams left_hip_pitch_params = new JointParams(0f, -2.5f, 2.5f, 10f, 200f);
    public JointParams left_knee_params = new JointParams(0f, -0.3f, 2.6f, 10f, 300f);
    public JointParams left_ankle_params = new JointParams(0f, -0.9f, 0.9f, 10f, 40f);
    
    public JointParams right_hip_yaw_params = new JointParams(0f, -2.9f, 2.9f, 10f, 200f);
    public JointParams right_hip_roll_params = new JointParams(0f, -0.5f, 0.5f, 10f, 200f);
    public JointParams right_hip_pitch_params = new JointParams(0f, -2.5f, 2.5f, 10f, 200f);
    public JointParams right_knee_params = new JointParams(0f, -0.3f, 2.6f, 10f, 300f);
    public JointParams right_ankle_params = new JointParams(0f, -0.9f, 0.9f, 10f, 40f);
    
    public JointParams torso_params = new JointParams(0f, -2.2f, 2.2f, 10f, 200f);
    
    public JointParams left_shoulder_pitch_params = new JointParams(0f, -2.9f, 2.9f, 10f, 100f);
    public JointParams left_shoulder_roll_params = new JointParams(0f, -1.8f, 2.9f, 10f, 100f);
    public JointParams left_shoulder_yaw_params = new JointParams(0f, -1.3f, 3.7f, 10f, 50f);
    public JointParams left_elbow_params = new JointParams(0f, -1.8f, 0.0f, 10f, 50f);
    
    public JointParams right_shoulder_pitch_params = new JointParams(0f, -2.9f, 2.9f, 10f, 100f);
    public JointParams right_shoulder_roll_params = new JointParams(0f, -2.9f, 1.8f, 10f, 100f);
    public JointParams right_shoulder_yaw_params = new JointParams(0f, -3.7f, 1.3f, 10f, 50f);
    public JointParams right_elbow_params = new JointParams(0f, 0.0f, 1.8f, 10f, 50f);

    // 内部状态变量
    private Vector3 base_lin_vel;
    private Vector3 base_ang_vel;
    private Vector3 base_lin_acc;  // 线加速度
    private Vector3 base_ang_acc;  // 角加速度
    private Vector3 projected_gravity;
    private float base_height;
    private bool reset_buf = false;
    private bool time_out_buf = false;
    
    // 用于计算速度和加速度的历史数据
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastLinearVelocity;
    private Vector3 lastAngularVelocity;
    private bool isFirstFrame = true;
    
    // 关节字典映射
    private Dictionary<string, MjActuator> jointActuators;
    private Dictionary<string, MjHingeJoint> jointHinges;
    private Dictionary<string, JointParams> jointParams;
    private Dictionary<string, float> jointPositions;
    private Dictionary<string, float> jointVelocities;
    private Dictionary<string, float> jointTorques;
    private Dictionary<string, float> jointActions;
    private Dictionary<string, float> lastJointActions;
    private Dictionary<string, float> lastJointVelocities;
    
    // 关节名称列表（按执行器顺序）
    // 注意：公开字段的命名约定是 jointName_actuator, jointName_joint, jointName_params
    private readonly string[] jointNames = new string[]
    {
        "left_hip_yaw", "left_hip_roll", "left_hip_pitch", "left_knee", "left_ankle",
        "right_hip_yaw", "right_hip_roll", "right_hip_pitch", "right_knee", "right_ankle",
        "torso",
        "left_shoulder_pitch", "left_shoulder_roll", "left_shoulder_yaw", "left_elbow",
        "right_shoulder_pitch", "right_shoulder_roll", "right_shoulder_yaw", "right_elbow"
    };
    
    private float[] feet_air_time;
    private bool[] last_contacts;
    
    // 反射缓存
    private readonly Dictionary<string, System.Reflection.FieldInfo> fieldCache = new Dictionary<string, System.Reflection.FieldInfo>();
    
    // 日志辅助方法
    private void LogDebug(string message) { if (showDebugLogs) Debug.Log(message); }
    private void LogWarning(string message) { if (showDebugLogs) Debug.LogWarning(message); }

    private void Start()
    {
        // 如果没有手动指定rootBody，尝试在父级中查找
        if (rootBody == null)
        {
            rootBody = GetComponentInParent<MjBaseBody>();
            if (rootBody == null && showDebugLogs)
            {
                Debug.LogWarning("未找到MjBaseBody组件！将使用头部Transform计算速度。");
            }
        }
        
        // 初始化历史数据
        if (head_self != null)
        {
            lastPosition = head_self.position;
            lastRotation = head_self.rotation;
        }
        lastLinearVelocity = Vector3.zero;
        lastAngularVelocity = Vector3.zero;
        
        if (autoInitializeComponents)
        {
            AutoInitializeJointComponents();
        }
        InitializeJointSystem();
        
        if (feet_transforms != null)
        {
            feet_air_time = new float[feet_transforms.Length];
            last_contacts = new bool[feet_transforms.Length];
        }
        
        // 在初始化后打印所有关节参数
        StartCoroutine(PrintJointParamsAfterInit());
    }
    
    public override void OnEpisodeBegin()
    {
        // Debug.Log("OnEpisodeBegin");
        
        // 使用MuJoCo的mj_resetData重置整个物理场景到初始状态
        var mjScene = MjScene.Instance;
        unsafe
        {
            if (mjScene != null && mjScene.Model != null && mjScene.Data != null)
            {
                // 重置MuJoCo数据到初始状态
                MujocoLib.mj_resetData(mjScene.Model, mjScene.Data);
                
                // 更新运动学以确保变换正确
                MujocoLib.mj_kinematics(mjScene.Model, mjScene.Data);
                
                // 同步Unity变换到MuJoCo状态
                mjScene.SyncUnityToMjState();
            }
        }
        
        // 重置机器人到原点
        ResetRobotPose();
        
        // 重置内部状态跟踪
        ResetVelocityStates();
        ResetJointStates();
        ResetFootStates();
        GenerateRandomCommand();
        
        reset_buf = false;
        time_out_buf = false;
    }
    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        CheckTermination();
        SavePreviousStates();
        ApplyActions(actionBuffers);
        SetReward(CalculateRewards());
    }
    
    private System.Collections.IEnumerator PrintJointParamsAfterInit()
    {
        if (!showDebugLogs) yield break;
        
        // 等待一帧以确保所有初始化完成
        yield return null;
        
        LogDebug("===== 关节参数最终值 =====");
        foreach (string jointName in jointNames)
        {
            if (jointParams.ContainsKey(jointName) && jointParams[jointName] != null)
            {
                var param = jointParams[jointName];
                LogDebug($"{jointName}: " +
                         $"位置限制=[{param.positionLowerLimit:F3}, {param.positionUpperLimit:F3}] rad, " +
                         $"速度限制={param.velocityLimit:F1} rad/s, " +
                         $"扭矩限制={param.torqueLimit:F1} N·m, " +
                         $"控制范围=[{param.controlRange.x:F2}, {param.controlRange.y:F2}]");
            }
        }
        LogDebug("==========================");
    }

    /// <summary>
    /// 自动初始化关节组件
    /// 支持MjActuator和MjHingeJoint分别在不同GameObject上的情况
    /// </summary>
    private void AutoInitializeJointComponents()
    {
        LogDebug($"开始自动初始化关节组件... (匹配模式: {(useLooseMatching ? "宽松" : "精确")})");
        int actuatorCount = 0;
        int hingeJointCount = 0;
        
        foreach (string jointName in jointNames)
        {
            // 分别查找包含MjActuator和MjHingeJoint的GameObject
            bool foundActuator = FindAndSetComponentByName<MjActuator>(jointName, "_actuator");
            bool foundHingeJoint = FindAndSetComponentByName<MjHingeJoint>(jointName, "_joint");
            
            if (foundActuator) actuatorCount++;
            if (foundHingeJoint) hingeJointCount++;
            
            if (!foundActuator && !foundHingeJoint)
            {
                LogWarning($"未找到名为 '{jointName}' 的任何相关组件");
            }
        }
        
        LogDebug($"自动初始化完成: 找到 {actuatorCount} 个MjActuator组件，{hingeJointCount} 个MjHingeJoint组件");
    }
    
    /// <summary>
    /// 在所有子物体中查找包含指定组件且名称匹配的GameObject
    /// </summary>
    private bool FindAndSetComponentByName<T>(string jointName, string fieldSuffix) where T : Component
    {
        // 获取所有子物体中的指定类型组件
        T[] allComponents = GetComponentsInChildren<T>();
        
        // 首先尝试精确匹配
        foreach (T component in allComponents)
        {
            if (component.gameObject.name == jointName)
            {
                return TrySetComponentField(component, jointName, fieldSuffix);
            }
        }
        
        // 如果启用宽松匹配且精确匹配失败，尝试包含匹配
        if (useLooseMatching)
        {
            foreach (T component in allComponents)
            {
                if (component.gameObject.name.Contains(jointName))
                {
                    LogDebug($"使用宽松匹配: GameObject '{component.gameObject.name}' 包含 '{jointName}'");
                    return TrySetComponentField(component, jointName, fieldSuffix);
                }
            }
        }
        
        LogWarning($"未找到名为 '{jointName}' 的 {typeof(T).Name} 组件");
        return false;
    }
    
    /// <summary>
    /// 尝试使用反射设置组件字段
    /// </summary>
    private bool TrySetComponentField<T>(T component, string jointName, string fieldSuffix) where T : Component
    {
        var field = GetCachedField(jointName + fieldSuffix);
            
        if (field != null)
        {
            field.SetValue(this, component);
            LogDebug($"已找到并设置 {jointName} 的 {typeof(T).Name} 组件 (来自GameObject: {component.gameObject.name})");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 获取缓存的字段信息
    /// </summary>
    private System.Reflection.FieldInfo GetCachedField(string fieldName)
    {
        if (!fieldCache.ContainsKey(fieldName))
        {
            var field = GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            fieldCache[fieldName] = field;
        }
        return fieldCache[fieldName];
    }



    private void InitializeJointSystem()
    {
        // 初始化所有字典
        jointActuators = new Dictionary<string, MjActuator>();
        jointHinges = new Dictionary<string, MjHingeJoint>();
        jointParams = new Dictionary<string, JointParams>();
        jointPositions = new Dictionary<string, float>();
        jointVelocities = new Dictionary<string, float>();
        jointTorques = new Dictionary<string, float>();
        jointActions = new Dictionary<string, float>();
        lastJointActions = new Dictionary<string, float>();
        lastJointVelocities = new Dictionary<string, float>();
        
        foreach (string jointName in jointNames)
        {
            // 使用缓存的反射获取对应的组件和参数
            var actuatorField = GetCachedField(jointName + "_actuator");
            var hingeField = GetCachedField(jointName + "_joint");
            var paramField = GetCachedField(jointName + "_params");
            
            if (actuatorField != null)
                jointActuators[jointName] = actuatorField.GetValue(this) as MjActuator;
            if (hingeField != null)
                jointHinges[jointName] = hingeField.GetValue(this) as MjHingeJoint;
            if (paramField != null && paramField.FieldType == typeof(JointParams))
                jointParams[jointName] = paramField.GetValue(this) as JointParams;
            
            // 初始化状态值
            jointPositions[jointName] = 0f;
            jointVelocities[jointName] = 0f;
            jointTorques[jointName] = 0f;
            jointActions[jointName] = 0f;
            lastJointActions[jointName] = 0f;
            lastJointVelocities[jointName] = 0f;
        }
        
        // 使用MjHingeJoint的参数更新JointParams
        if (autoUpdateJointParamsFromHinge)
        {
            UpdateJointParamsFromHingeJoints();
            UpdateJointParamsFromActuators();
            SyncJointParamsToFields();
        }
    }
    
    /// <summary>
    /// 从MjHingeJoint组件读取参数并更新对应的JointParams
    /// 适用于从URDF/MJCF导入的模型，可以自动获取关节限制
    /// 注意：速度限制和扭矩限制需要手动设置，因为MjHingeJoint不包含这些信息
    /// </summary>
    private void UpdateJointParamsFromHingeJoints()
    {
        LogDebug("正在从MjHingeJoint组件自动更新关节参数...");
        int updatedCount = 0;
        
        foreach (string jointName in jointNames)
        {
            if (jointHinges.TryGetValue(jointName, out var hingeJoint) && hingeJoint != null &&
                jointParams.TryGetValue(jointName, out var param) && param != null)
            {
                // 更新位置限制（从度转换为弧度）
                param.positionLowerLimit = hingeJoint.RangeLower * Mathf.Deg2Rad;
                param.positionUpperLimit = hingeJoint.RangeUpper * Mathf.Deg2Rad;
                
                // 更新默认位置（从度转换为弧度）
                param.defaultPosition = hingeJoint.Configuration * Mathf.Deg2Rad;
                
                // 如果关节有限制，确保参数反映这一点
                if (hingeJoint.Settings.Solver.Limited &&
                    Mathf.Approximately(param.positionLowerLimit, param.positionUpperLimit))
                {
                    LogWarning($"关节 {jointName} 被标记为有限制，但上下限相等。保持原有限制。");
                }
                
                LogDebug($"已从MjHingeJoint更新 {jointName} 的参数: " +
                         $"位置限制=[{param.positionLowerLimit:F3}, {param.positionUpperLimit:F3}] rad, " +
                         $"默认位置={param.defaultPosition:F3} rad");
                
                updatedCount++;
            }
        }
        
        LogDebug($"关节参数更新完成。共更新了 {updatedCount}/{jointNames.Length} 个关节的参数。");
    }
    

    
    /// <summary>
    /// 将字典中的JointParams值同步回公开字段，以便在Inspector中显示
    /// </summary>
    private void SyncJointParamsToFields()
    {
        LogDebug("正在同步关节参数到公开字段...");
        int syncedCount = 0;
        
        foreach (string jointName in jointNames)
        {
            if (jointParams.TryGetValue(jointName, out var param) && param != null)
            {
                var field = GetCachedField(jointName + "_params");
                    
                if (field != null && field.FieldType == typeof(JointParams))
                {
                    var fieldParam = field.GetValue(this) as JointParams;
                    if (fieldParam != null)
                    {
                        // 复制更新后的值到字段
                        fieldParam.defaultPosition = param.defaultPosition;
                        fieldParam.positionLowerLimit = param.positionLowerLimit;
                        fieldParam.positionUpperLimit = param.positionUpperLimit;
                        fieldParam.velocityLimit = param.velocityLimit;
                        fieldParam.torqueLimit = param.torqueLimit;
                        fieldParam.controlRange = param.controlRange;
                        
                        syncedCount++;
                    }
                }
            }
        }
        
        LogDebug($"参数同步完成，已同步 {syncedCount} 个关节参数");
    }
    
    /// <summary>
    /// 从MjActuator组件读取参数并更新对应的JointParams
    /// 主要更新扭矩限制信息
    /// </summary>
    private void UpdateJointParamsFromActuators()
    {
        LogDebug("正在从MjActuator组件更新关节参数...");
        int updatedCount = 0;
        
        foreach (string jointName in jointNames)
        {
            if (jointActuators.TryGetValue(jointName, out var actuator) && actuator != null &&
                jointParams.TryGetValue(jointName, out var param) && param != null)
            {
                // 保存控制范围
                if (actuator.CommonParams.CtrlLimited && actuator.CommonParams.CtrlRange != Vector2.zero)
                {
                    param.controlRange = actuator.CommonParams.CtrlRange;
                    LogDebug($"从MjActuator更新 {jointName} 的控制范围: [{param.controlRange.x:F2}, {param.controlRange.y:F2}]");
                }
                else
                {
                    // 如果没有限制，使用默认的[-1, 1]范围
                    param.controlRange = new Vector2(-1f, 1f);
                }
                
                // 从ForceRange获取扭矩限制
                if (actuator.CommonParams.ForceLimited && actuator.CommonParams.ForceRange != Vector2.zero)
                {
                    float maxForce = Mathf.Max(Mathf.Abs(actuator.CommonParams.ForceRange.x), 
                                              Mathf.Abs(actuator.CommonParams.ForceRange.y));
                    if (maxForce > 0)
                    {
                        param.torqueLimit = maxForce;
                        LogDebug($"从MjActuator更新 {jointName} 的扭矩限制: {maxForce:F2} N·m");
                    }
                }
                
                // 对于速度执行器，尝试从控制范围获取速度限制
                if (actuator.Type == MjActuator.ActuatorType.Velocity && 
                    actuator.CommonParams.CtrlLimited && 
                    actuator.CommonParams.CtrlRange != Vector2.zero)
                {
                    float maxVel = Mathf.Max(Mathf.Abs(actuator.CommonParams.CtrlRange.x),
                                            Mathf.Abs(actuator.CommonParams.CtrlRange.y));
                    if (maxVel > 0)
                    {
                        param.velocityLimit = maxVel;
                        LogDebug($"从速度执行器更新 {jointName} 的速度限制: {maxVel:F2} rad/s");
                    }
                }
                
                updatedCount++;
            }
        }
        
        LogDebug($"执行器参数更新完成。共更新了 {updatedCount}/{jointNames.Length} 个关节的参数。");
        if (showDebugLogs && updatedCount > 0)
        {
            LogWarning("注意：速度限制通常需要从MJCF/URDF文件中获取，MjActuator只能提供有限的信息。");
        }
    }

    /// <summary>
    /// 收集观察数据供神经网络使用
    /// 总计收集: 3(线速度) + 3(角速度) + 3(重力) + 3(命令) + 19(关节位置) + 19(关节速度) + 19(上一步动作) = 69维
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        UpdateInternalStates();
        
        // 基础状态观察
        sensor.AddObservation(base_lin_vel * obs_scales.lin_vel);
        sensor.AddObservation(base_ang_vel * obs_scales.ang_vel);
        sensor.AddObservation(projected_gravity);
        sensor.AddObservation(commands * obs_scales.commands_scale);
        
        // 关节观察
        foreach (string jointName in jointNames)
        {
            // 位置偏差
            float positionDeviation = 0f;
            if (jointPositions.TryGetValue(jointName, out float pos) && 
                jointParams.TryGetValue(jointName, out var param) && param != null)
            {
                positionDeviation = pos - param.defaultPosition;
            }
            sensor.AddObservation(positionDeviation * obs_scales.dof_pos);
            
            // 速度
            float velocity = jointVelocities.TryGetValue(jointName, out float vel) ? vel : 0f;
            sensor.AddObservation(velocity * obs_scales.dof_vel);
            
            // 上一步动作
            float lastAction = lastJointActions.TryGetValue(jointName, out float action) ? action : 0f;
            sensor.AddObservation(lastAction);
        }
    }

    private void UpdateInternalStates()
    {
        // 更新基础状态
        if (head_self != null)
        {
            UpdateVelocitiesFromTransform();
            // 使用全局坐标系中头部位置的y坐标作为base_height
            base_height = head_self.position.y;
            // Debug.Log("base_height: " + base_height);
        }
        
        projected_gravity = transform.InverseTransformDirection(Vector3.down);
        
        // 更新所有关节状态
        foreach (string jointName in jointNames)
        {
            UpdateJointState(jointName);
        }
        
        // 更新脚部接触状态
        UpdateFootContactStates();
    }

    private void UpdateJointState(string jointName)
    {
        var actuator = jointActuators[jointName];
        var hingeJoint = jointHinges[jointName];
        
        if (hingeJoint != null)
        {
            // 从MjHingeJoint获取真实的关节数据
            jointPositions[jointName] = (float)hingeJoint.RawConfiguration;
            jointVelocities[jointName] = hingeJoint.Velocity;
            
            if (actuator != null)
            {
                jointTorques[jointName] = actuator.Control;
            }
        }
        else if (actuator != null)
        {
            // 备用方案：通过actuator估算
            float newPosition = actuator.Control / force_multiplier;
            float lastPos = jointPositions.ContainsKey(jointName) ? jointPositions[jointName] : 0f;
            
            jointVelocities[jointName] = (newPosition - lastPos) / dt;
            jointPositions[jointName] = newPosition;
            jointTorques[jointName] = actuator.Control;
            
            if (showDebugLogs && isFirstFrame)
            {
                LogWarning($"关节 {jointName} 没有MjHingeJoint组件，使用备用方案");
            }
        }
    }

    private void UpdateFootContactStates()
    {
        if (feet_transforms == null || feet_air_time == null) return;
        
        for (int i = 0; i < feet_transforms.Length; i++)
        {
            bool is_contact = CheckFootContact(feet_transforms[i]);
            feet_air_time[i] = is_contact ? 0f : feet_air_time[i] + dt;
            last_contacts[i] = is_contact;
        }
    }

    private bool CheckFootContact(Transform foot)
    {
        // 简单的地面接触检测
        return Physics.Raycast(foot.position, Vector3.down, 0.1f);
    }

    private void UpdateVelocitiesFromTransform()
    {
        if (isFirstFrame)
        {
            // 第一帧只记录位置和旋转，不计算速度
            lastPosition = head_self.position;
            lastRotation = head_self.rotation;
            base_lin_vel = Vector3.zero;
            base_ang_vel = Vector3.zero;
            base_lin_acc = Vector3.zero;
            base_ang_acc = Vector3.zero;
            isFirstFrame = false;
            return;
        }
        
        // 计算线速度（世界坐标系）
        Vector3 currentPosition = head_self.position;
        base_lin_vel = (currentPosition - lastPosition) / dt;
        
        // 计算角速度（局部坐标系）
        Quaternion deltaRotation = head_self.rotation * Quaternion.Inverse(lastRotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
        
        // 处理角度范围 (-180, 180]
        if (angle > 180f)
        {
            angle -= 360f;
        }
        
        // 将角速度转换到局部坐标系
        float angularSpeed = angle * Mathf.Deg2Rad / dt;
        base_ang_vel = head_self.InverseTransformDirection(axis * angularSpeed);
        
        // 计算线加速度
        base_lin_acc = (base_lin_vel - lastLinearVelocity) / dt;
        
        // 计算角加速度
        base_ang_acc = (base_ang_vel - lastAngularVelocity) / dt;
        
        // 更新历史数据
        lastPosition = currentPosition;
        lastRotation = head_self.rotation;
        lastLinearVelocity = base_lin_vel;
        lastAngularVelocity = base_ang_vel;
    }

    private void ResetRobotPose()
    {
        // 对于固定基座的机器人，只需重置Unity Transform
        transform.position = new Vector3(0f, resetHeight, 0f);
        transform.rotation = Quaternion.identity;
        
        // 如果机器人有free joint（自由基座），需要同时更新MuJoCo的qpos
        // 注意：对于Unitree H1这样的人形机器人，通常有一个free joint作为根关节
        if (rootBody != null)
        {
            var freeJoint = rootBody.GetComponentInChildren<MjFreeJoint>();
            if (freeJoint != null && freeJoint.QposAddress >= 0)
            {
                var mjScene = MjScene.Instance;
                unsafe
                {
                    if (mjScene != null && mjScene.Data != null)
                    {
                        // Free joint的qpos布局：[pos_x, pos_y, pos_z, quat_w, quat_x, quat_y, quat_z]
                        mjScene.Data->qpos[freeJoint.QposAddress] = 0.0;     // x
                        mjScene.Data->qpos[freeJoint.QposAddress + 1] = 0.0; // y
                        mjScene.Data->qpos[freeJoint.QposAddress + 2] = resetHeight; // z
                        mjScene.Data->qpos[freeJoint.QposAddress + 3] = 1.0; // quat_w
                        mjScene.Data->qpos[freeJoint.QposAddress + 4] = 0.0; // quat_x
                        mjScene.Data->qpos[freeJoint.QposAddress + 5] = 0.0; // quat_y
                        mjScene.Data->qpos[freeJoint.QposAddress + 6] = 0.0; // quat_z
                        
                        // 重置速度（6个自由度）
                        if (freeJoint.DofAddress >= 0)
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                mjScene.Data->qvel[freeJoint.DofAddress + i] = 0.0;
                            }
                        }
                    }
                }
            }
        }
    }

    private void ResetVelocityStates()
    {
        isFirstFrame = true;
        base_lin_vel = Vector3.zero;
        base_ang_vel = Vector3.zero;
        base_lin_acc = Vector3.zero;
        base_ang_acc = Vector3.zero;
        lastLinearVelocity = Vector3.zero;
        lastAngularVelocity = Vector3.zero;
        
        if (head_self != null)
        {
            lastPosition = head_self.position;
            lastRotation = head_self.rotation;
        }
    }

    private void ResetJointStates()
    {
        foreach (string jointName in jointNames)
        {
            lastJointActions[jointName] = 0f;
            lastJointVelocities[jointName] = 0f;
            jointActions[jointName] = 0f;
            
            // 如果需要设置特定的关节位置（而不是使用mj_resetData的默认值）
            if (jointHinges.TryGetValue(jointName, out var hingeJoint) && hingeJoint != null &&
                jointParams.TryGetValue(jointName, out var param) && param != null)
            {
                // 可以通过直接修改MuJoCo的qpos数据来设置关节位置
                var mjScene = MjScene.Instance;
                unsafe
                {
                    if (mjScene != null && mjScene.Data != null && hingeJoint.QposAddress >= 0)
                    {
                        // 设置关节位置到默认值
                        mjScene.Data->qpos[hingeJoint.QposAddress] = param.defaultPosition;
                        
                        // 设置关节速度为0
                        if (hingeJoint.DofAddress >= 0)
                        {
                            mjScene.Data->qvel[hingeJoint.DofAddress] = 0.0;
                        }
                    }
                }
            }
            
            jointPositions[jointName] = jointParams[jointName].defaultPosition;
            jointVelocities[jointName] = 0f;
        }
        
        // 更新运动学
        var mjScene2 = MjScene.Instance;
        unsafe
        {
            if (mjScene2 != null && mjScene2.Model != null && mjScene2.Data != null)
            {
                MujocoLib.mj_kinematics(mjScene2.Model, mjScene2.Data);
            }
        }
    }

    private void ResetFootStates()
    {
        if (feet_air_time != null)
            System.Array.Clear(feet_air_time, 0, feet_air_time.Length);
    }

    private void GenerateRandomCommand()
    {
        // y是垂直方向，x是前进方向，z是旋转方向
        commands = new Vector3(
            UnityEngine.Random.Range(-1f, 1f), // x velocity
            // UnityEngine.Random.Range(-0f, 0f), // y velocity  
            1.75f,
            UnityEngine.Random.Range(-1f, 1f)  // z velocity
        );
    }

    private void SavePreviousStates()
    {
        foreach (string jointName in jointNames)
        {
            lastJointActions[jointName] = jointActions[jointName];
            lastJointVelocities[jointName] = jointVelocities[jointName];
        }
    }

    private void ApplyActions(ActionBuffers actionBuffers)
    {
        var actions = actionBuffers.ContinuousActions;
        int actionCount = Mathf.Min(jointNames.Length, actions.Length);
        
        // 关节分组说明：
        // 下半身关节（索引0-9）：左右腿各5个关节
        //   - left_hip_yaw (0), left_hip_roll (1), left_hip_pitch (2), left_knee (3), left_ankle (4)
        //   - right_hip_yaw (5), right_hip_roll (6), right_hip_pitch (7), right_knee (8), right_ankle (9)
        // 躯干关节（索引10）：torso
        // 上半身关节（索引11-18）：左右臂各4个关节
        //   - 左臂：left_shoulder_pitch (11), left_shoulder_roll (12), left_shoulder_yaw (13), left_elbow (14)
        //   - 右臂：right_shoulder_pitch (15), right_shoulder_roll (16), right_shoulder_yaw (17), right_elbow (18)
        
        // 使用配置的关节数量
        int controlActionCount = Mathf.Min(controlledJointCount, actions.Length);
        
        for (int i = 0; i < controlActionCount; i++)
        {
            string jointName = jointNames[i];
            float action = actions[i]; // 归一化的动作值 [-1, 1]
            jointActions[jointName] = action;
            
            if (jointActuators.TryGetValue(jointName, out var actuator) && actuator != null &&
                jointParams.TryGetValue(jointName, out var param) && param != null)
            {
                // 将归一化的动作值映射到控制范围
                // action = -1 映射到 controlRange.x (最小值)
                // action = 1 映射到 controlRange.y (最大值)
                float rangeCenter = (param.controlRange.x + param.controlRange.y) * 0.5f;
                float rangeHalf = (param.controlRange.y - param.controlRange.x) * 0.5f;
                float mappedControl = rangeCenter + action * rangeHalf;
                
                // 应用到执行器
                actuator.Control = mappedControl * force_multiplier;
                
            //     if (showDebugLogs && i == 0) // 只为第一个关节打印调试信息
            //     {
            //         LogDebug($"{jointName}: action={action:F3} -> control={mappedControl:F3} (range=[{param.controlRange.x:F2}, {param.controlRange.y:F2}])");
            //     }
            }
        }
        
        // 未控制的关节保持稳定
        for (int i = controlledJointCount; i < jointNames.Length; i++)
        {
            string jointName = jointNames[i];
            jointActions[jointName] = 0f; // 上半身关节动作设为0
            
            if (jointActuators.TryGetValue(jointName, out var actuator) && actuator != null &&
                jointParams.TryGetValue(jointName, out var param) && param != null)
            {
                // 上半身关节保持当前位置，设置控制信号为0
                // 这样MuJoCo会使用关节的内在阻尼来保持稳定
                actuator.Control = 0f;
            }
        }
    }

    /// <summary>
    /// 计算所有奖励的总和
    /// 包含18种不同的奖励/惩罚机制
    /// </summary>
    private float CalculateRewards()
    {
        float total_reward = 0f;
        
        // 基础运动奖励
        total_reward += rewards_config.lin_vel_z_weight * Squared(base_lin_vel.z);
        total_reward += rewards_config.ang_vel_xy_weight * (Squared(base_ang_vel.x) + Squared(base_ang_vel.y));
        total_reward += rewards_config.orientation_weight * (Squared(projected_gravity.x) + Squared(projected_gravity.y));
        total_reward += rewards_config.base_height_weight * Squared(base_height - rewards_config.base_height_target);
        
        // 关节相关奖励
        total_reward += rewards_config.torques_weight * SumJointValues(jointName => Squared(jointTorques[jointName]));
        total_reward += rewards_config.dof_vel_weight * SumJointValues(jointName => Squared(jointVelocities[jointName]));
        total_reward += rewards_config.dof_acc_weight * SumJointValues(jointName => 
        {
            float acc = (lastJointVelocities[jointName] - jointVelocities[jointName]) / dt;
            return Squared(acc);
        });
        total_reward += rewards_config.action_rate_weight * SumJointValues(jointName => 
            Squared(lastJointActions[jointName] - jointActions[jointName]));
        
        // 碰撞和终止奖励
        if (penalised_contact_bodies != null)
        {
            total_reward += rewards_config.collision_weight * CountCollisions(penalised_contact_bodies);
        }
        
        if (reset_buf && !time_out_buf)
        {
            total_reward += rewards_config.termination_weight;
        }
        
        // 关节限制惩罚
        total_reward += rewards_config.dof_pos_limits_weight * SumJointValues(jointName =>
        {
            float position = jointPositions[jointName];
            var param = jointParams[jointName];
            return Mathf.Max(0f, param.positionLowerLimit - position) + 
                   Mathf.Max(0f, position - param.positionUpperLimit);
        });
        
        total_reward += rewards_config.dof_vel_limits_weight * SumJointValues(jointName =>
        {
            float velocity = jointVelocities[jointName];
            var param = jointParams[jointName];
            float vel_violation = Mathf.Max(0f, Mathf.Abs(velocity) - param.velocityLimit * rewards_config.soft_dof_vel_limit);
            return Mathf.Min(vel_violation, 1f);
        });
        
        total_reward += rewards_config.torque_limits_weight * SumJointValues(jointName =>
        {
            float torque = jointTorques[jointName];
            var param = jointParams[jointName];
            return Mathf.Max(0f, Mathf.Abs(torque) - param.torqueLimit * rewards_config.soft_torque_limit);
        });
        
        // 速度跟踪奖励
        float lin_vel_error = Squared(commands.x - base_lin_vel.x) + Squared(commands.y - base_lin_vel.y);
        total_reward += rewards_config.tracking_lin_vel_weight * Mathf.Exp(-lin_vel_error / rewards_config.tracking_sigma);
        
        float ang_vel_error = Squared(commands.z - base_ang_vel.z);
        total_reward += rewards_config.tracking_ang_vel_weight * Mathf.Exp(-ang_vel_error / rewards_config.tracking_sigma);
        
        // 脚部相关奖励
        if (feet_transforms != null)
        {
            if (feet_air_time != null)
            {
                total_reward += rewards_config.feet_air_time_weight * 
                    feet_air_time.Sum(time => Mathf.Min(time, 0.5f));
            }
            
            total_reward += rewards_config.stumble_weight * CountStumbles(feet_transforms);
            
            total_reward += rewards_config.feet_contact_forces_weight * 
                feet_transforms.Sum(foot =>
                {
                    float force = GetContactForce(foot);
                    return Mathf.Max(0f, force - rewards_config.max_contact_force);
                });
        }
        
        // 静止状态惩罚
        if (commands.magnitude < 0.1f)
        {
            total_reward += rewards_config.stand_still_weight * SumJointValues(jointName =>
                Mathf.Abs(jointPositions[jointName] - jointParams[jointName].defaultPosition));
        }
        
        return total_reward;
    }

    // 辅助方法
    private float Squared(float value) => value * value;
    
    private float SumJointValues(System.Func<string, float> calculateValue)
    {
        float sum = 0f;
        foreach (string jointName in jointNames)
        {
            sum += calculateValue(jointName);
        }
        return sum;
    }
    
    private float CountCollisions(Transform[] bodies)
    {
        return bodies.Count(body => CheckCollision(body));
    }
    
    private float CountStumbles(Transform[] feet)
    {
        return feet.Count(foot => CheckStumble(foot));
    }

    private bool CheckCollision(Transform body)
    {
        // 简化的碰撞检测
        Collider collider = body.GetComponent<Collider>();
        if (collider != null)
        {
            return Physics.CheckBox(collider.bounds.center, collider.bounds.extents, collider.transform.rotation);
        }
        return false;
    }

    private bool CheckStumble(Transform foot)
    {
        // 检测脚是否撞到垂直表面
        RaycastHit hit;
        if (Physics.Raycast(foot.position, foot.forward, out hit, 0.1f))
        {
            return Vector3.Dot(hit.normal, Vector3.up) < 0.7f; // 如果表面不够水平则认为是绊倒
        }
        return false;
    }

    private float GetContactForce(Transform foot)
    {
        // 简化的接触力计算
        // 在实际实现中，这应该从物理引擎获取真实的接触力
        if (CheckFootContact(foot))
        {
            // 尝试获取机器人的总质量
            float totalMass = GetRobotMass();
            return totalMass * 9.81f; // 简单估算
        }
        return 0f;
    }

    private float GetRobotMass()
    {
        if (rootBody == null) return 50f; // 默认质量
        
        // 获取所有MjInertial组件的质量总和
        var inertials = rootBody.GetComponentsInChildren<MjInertial>();
        float totalMass = inertials.Sum(inertial => inertial.Mass);
        
        return totalMass > 0 ? totalMass : 50f;
    }

    private void CheckTermination()
    {
        bool shouldTerminate = IsHeightOutOfBounds() || IsExcessivelyTilted();
        // Debug.Log("base_height: " + base_height);
        // Debug.Log("IsHeightOutOfBounds(): " + IsHeightOutOfBounds());
        // Debug.Log("IsExcessivelyTilted(): " + IsExcessivelyTilted());
        // Debug.Log("shouldTerminate: " + shouldTerminate);
        
        if (shouldTerminate)
        {
            reset_buf = true;
            EndEpisode();
        }
    }

    private bool IsHeightOutOfBounds()
    {
        return base_height < 1f || base_height > 2.0f;
    }

    private bool IsExcessivelyTilted()
    {
        return Vector3.Dot(transform.up, Vector3.up) < 0.5f;
    }
}
