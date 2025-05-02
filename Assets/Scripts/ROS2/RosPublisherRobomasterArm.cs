using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.UnityRoboticsDemo;
using Mujoco;

/// <summary>
///
/// </summary>
public class RosPublisherRobomasterArm : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "status_robomaster_arm";
    public Transform robomaster_arm_root;

    public bool is_object_grabbed = false;

    // Publish the cube's position and rotation every N seconds
    public float publishMessageFrequency = 0.02f;

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    void Start()
    {
        // start the ROS connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<BoolMsg>(topicName);
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed > publishMessageFrequency)
        {

            BoolMsg is_object_grabbed_msg = new BoolMsg(
                is_object_grabbed
            );

            // Finally send the message to server_endpoint.py running in ROS
            ros.Publish(topicName, is_object_grabbed_msg);

            timeElapsed = 0;
        }
    }
}