using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
//using RosColor = RosMessageTypes.UnityRoboticsDemo.UnityColorMsg;
using RosH1ControlCommand = RosMessageTypes.UnityRoboticsDemo.H1ControlCommandMsg;
using Mujoco;

public class RosSubscriberH1 : MonoBehaviour
{
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

    void Start()
    {
        
        ROSConnection.GetOrCreateInstance().Subscribe<RosH1ControlCommand>("H1ControlCommand", H1ControlCommand);
    }

    void H1ControlCommand(RosH1ControlCommand ros_h1_control_command)
    {
        //left_hip_yaw.Control = (byte) ros_h1_control_command.left_hip_yaw;
        //left_hip_roll.Control = (byte)ros_h1_control_command.left_hip_roll;
        //left_hip_pitch.Control = (byte)ros_h1_control_command.left_hip_pitch;
        //left_knee.Control = (byte)ros_h1_control_command.left_knee;
        //left_ankle.Control = (byte)ros_h1_control_command.left_ankle;
        //right_hip_yaw.Control = (byte)ros_h1_control_command.right_hip_yaw;
        //right_hip_roll.Control = (byte)ros_h1_control_command.right_hip_roll;
        //right_hip_pitch.Control = (byte)ros_h1_control_command.right_hip_pitch;
        //right_knee.Control = (byte)ros_h1_control_command.right_knee;
        //right_ankle.Control = (byte)ros_h1_control_command.right_ankle;
        //torso.Control = (byte)ros_h1_control_command.torso;
        //left_shoulder_pitch.Control = (byte)ros_h1_control_command.left_shoulder_pitch;
        //left_shoulder_roll.Control = (byte)ros_h1_control_command.left_shoulder_roll;
        //left_shoulder_yaw.Control = (byte)ros_h1_control_command.left_shoulder_yaw;
        //left_elbow.Control = (byte)ros_h1_control_command.left_elbow;

        //right_shoulder_pitch.Control = (float)ros_h1_control_command.right_shoulder_pitch * Mathf.Rad2Deg;
        //right_shoulder_roll.Control = (float)ros_h1_control_command.right_shoulder_roll * Mathf.Rad2Deg;
        //right_shoulder_yaw.Control = (float)ros_h1_control_command.right_shoulder_yaw * Mathf.Rad2Deg;
        //right_elbow.Control = (float)ros_h1_control_command.right_elbow * Mathf.Rad2Deg;


        //right_shoulder_pitch.Control = (float)ros_h1_control_command.right_shoulder_pitch * 5;
        //right_shoulder_roll.Control = (float)ros_h1_control_command.right_shoulder_roll * 2;
        //right_shoulder_yaw.Control = (float)ros_h1_control_command.right_shoulder_yaw * 2;
        //right_elbow.Control = (float)ros_h1_control_command.right_elbow * -2;

        right_shoulder_pitch.Control = (float)ros_h1_control_command.right_shoulder_pitch;
        right_shoulder_roll.Control = (float)ros_h1_control_command.right_shoulder_roll;
        right_shoulder_yaw.Control = (float)ros_h1_control_command.right_shoulder_yaw;
        right_elbow.Control = (float)ros_h1_control_command.right_elbow;
        //Debug.Log(right_shoulder_roll.Control);
        //cube.GetComponent<Renderer>().material.color = new Color32((byte)colorMessage.r, (byte)colorMessage.g, (byte)colorMessage.b, (byte)colorMessage.a);
    }
}