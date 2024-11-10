using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.UnityRoboticsDemo;
using Mujoco;

/// <summary>
///
/// </summary>
public class RosPublisherH1 : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "status_h1";
    public Transform h1_root;

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

    // Publish the cube's position and rotation every N seconds
    public float publishMessageFrequency = 0.02f;

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    void Start()
    {
        // start the ROS connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PosRotMsg>(topicName);
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed > publishMessageFrequency)
        {

            PosRotMsg H1Pos = new PosRotMsg(
                h1_root.transform.position.x,
                h1_root.transform.position.y,
                h1_root.transform.position.z,
                h1_root.transform.rotation.x,
                h1_root.transform.rotation.y,
                h1_root.transform.rotation.z,
                h1_root.transform.rotation.w
            );

            // Finally send the message to server_endpoint.py running in ROS
            ros.Publish(topicName, H1Pos);

            timeElapsed = 0;
        }
    }
}