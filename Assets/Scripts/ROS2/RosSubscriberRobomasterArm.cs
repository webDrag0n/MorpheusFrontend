using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
//using RosColor = RosMessageTypes.UnityRoboticsDemo.UnityColorMsg;
using RosFloat64MultiArray = RosMessageTypes.UnityRoboticsDemo.Float64MultiArrayMsg;
using Mujoco;

public class RosSubscriberRobomasterArm : MonoBehaviour
{
    public MjActuator general_16;
    public MjActuator general_17;
    public MjActuator general_18;
    public MjActuator general_19;
    public MjActuator general_20;
    public MjActuator general_21;
    public MjActuator general_22;

    void Start()
    {
        
        ROSConnection.GetOrCreateInstance().Subscribe<RosFloat64MultiArray>("Float64MultiArray", RobomasterArmControlCommand);
    }

    void RobomasterArmControlCommand(RosFloat64MultiArray ros_robomaster_arm_control_command)
    {
        general_16.Control = (float)ros_robomaster_arm_control_command.data[0];
        general_17.Control = (float)ros_robomaster_arm_control_command.data[1];
        general_18.Control = (float)ros_robomaster_arm_control_command.data[2];
        general_19.Control = (float)ros_robomaster_arm_control_command.data[3];
        general_20.Control = (float)ros_robomaster_arm_control_command.data[4];
        general_21.Control = (float)ros_robomaster_arm_control_command.data[5];
        general_22.Control = (float)ros_robomaster_arm_control_command.data[6];
    }
}