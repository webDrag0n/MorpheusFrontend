//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.UnityRoboticsDemo
{
    [Serializable]
    public class H1ControlCommandMsg : Message
    {
        public const string k_RosMessageName = "unity_robotics_demo_msgs/H1ControlCommand";
        public override string RosMessageName => k_RosMessageName;

        public float left_hip_yaw;
        public float left_hip_roll;
        public float left_hip_pitch;
        public float left_knee;
        public float left_ankle;
        public float right_hip_yaw;
        public float right_hip_roll;
        public float right_hip_pitch;
        public float right_knee;
        public float right_ankle;
        public float torso;
        public float left_shoulder_pitch;
        public float left_shoulder_roll;
        public float left_shoulder_yaw;
        public float left_elbow;
        public float right_shoulder_pitch;
        public float right_shoulder_roll;
        public float right_shoulder_yaw;
        public float right_elbow;

        public H1ControlCommandMsg()
        {
            this.left_hip_yaw = 0.0f;
            this.left_hip_roll = 0.0f;
            this.left_hip_pitch = 0.0f;
            this.left_knee = 0.0f;
            this.left_ankle = 0.0f;
            this.right_hip_yaw = 0.0f;
            this.right_hip_roll = 0.0f;
            this.right_hip_pitch = 0.0f;
            this.right_knee = 0.0f;
            this.right_ankle = 0.0f;
            this.torso = 0.0f;
            this.left_shoulder_pitch = 0.0f;
            this.left_shoulder_roll = 0.0f;
            this.left_shoulder_yaw = 0.0f;
            this.left_elbow = 0.0f;
            this.right_shoulder_pitch = 0.0f;
            this.right_shoulder_roll = 0.0f;
            this.right_shoulder_yaw = 0.0f;
            this.right_elbow = 0.0f;
        }

        public H1ControlCommandMsg(float left_hip_yaw, float left_hip_roll, float left_hip_pitch, float left_knee, float left_ankle, float right_hip_yaw, float right_hip_roll, float right_hip_pitch, float right_knee, float right_ankle, float torso, float left_shoulder_pitch, float left_shoulder_roll, float left_shoulder_yaw, float left_elbow, float right_shoulder_pitch, float right_shoulder_roll, float right_shoulder_yaw, float right_elbow)
        {
            this.left_hip_yaw = left_hip_yaw;
            this.left_hip_roll = left_hip_roll;
            this.left_hip_pitch = left_hip_pitch;
            this.left_knee = left_knee;
            this.left_ankle = left_ankle;
            this.right_hip_yaw = right_hip_yaw;
            this.right_hip_roll = right_hip_roll;
            this.right_hip_pitch = right_hip_pitch;
            this.right_knee = right_knee;
            this.right_ankle = right_ankle;
            this.torso = torso;
            this.left_shoulder_pitch = left_shoulder_pitch;
            this.left_shoulder_roll = left_shoulder_roll;
            this.left_shoulder_yaw = left_shoulder_yaw;
            this.left_elbow = left_elbow;
            this.right_shoulder_pitch = right_shoulder_pitch;
            this.right_shoulder_roll = right_shoulder_roll;
            this.right_shoulder_yaw = right_shoulder_yaw;
            this.right_elbow = right_elbow;
        }

        public static H1ControlCommandMsg Deserialize(MessageDeserializer deserializer) => new H1ControlCommandMsg(deserializer);

        private H1ControlCommandMsg(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.left_hip_yaw);
            deserializer.Read(out this.left_hip_roll);
            deserializer.Read(out this.left_hip_pitch);
            deserializer.Read(out this.left_knee);
            deserializer.Read(out this.left_ankle);
            deserializer.Read(out this.right_hip_yaw);
            deserializer.Read(out this.right_hip_roll);
            deserializer.Read(out this.right_hip_pitch);
            deserializer.Read(out this.right_knee);
            deserializer.Read(out this.right_ankle);
            deserializer.Read(out this.torso);
            deserializer.Read(out this.left_shoulder_pitch);
            deserializer.Read(out this.left_shoulder_roll);
            deserializer.Read(out this.left_shoulder_yaw);
            deserializer.Read(out this.left_elbow);
            deserializer.Read(out this.right_shoulder_pitch);
            deserializer.Read(out this.right_shoulder_roll);
            deserializer.Read(out this.right_shoulder_yaw);
            deserializer.Read(out this.right_elbow);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.left_hip_yaw);
            serializer.Write(this.left_hip_roll);
            serializer.Write(this.left_hip_pitch);
            serializer.Write(this.left_knee);
            serializer.Write(this.left_ankle);
            serializer.Write(this.right_hip_yaw);
            serializer.Write(this.right_hip_roll);
            serializer.Write(this.right_hip_pitch);
            serializer.Write(this.right_knee);
            serializer.Write(this.right_ankle);
            serializer.Write(this.torso);
            serializer.Write(this.left_shoulder_pitch);
            serializer.Write(this.left_shoulder_roll);
            serializer.Write(this.left_shoulder_yaw);
            serializer.Write(this.left_elbow);
            serializer.Write(this.right_shoulder_pitch);
            serializer.Write(this.right_shoulder_roll);
            serializer.Write(this.right_shoulder_yaw);
            serializer.Write(this.right_elbow);
        }

        public override string ToString()
        {
            return "H1ControlCommandMsg: " +
            "\nleft_hip_yaw: " + left_hip_yaw.ToString() +
            "\nleft_hip_roll: " + left_hip_roll.ToString() +
            "\nleft_hip_pitch: " + left_hip_pitch.ToString() +
            "\nleft_knee: " + left_knee.ToString() +
            "\nleft_ankle: " + left_ankle.ToString() +
            "\nright_hip_yaw: " + right_hip_yaw.ToString() +
            "\nright_hip_roll: " + right_hip_roll.ToString() +
            "\nright_hip_pitch: " + right_hip_pitch.ToString() +
            "\nright_knee: " + right_knee.ToString() +
            "\nright_ankle: " + right_ankle.ToString() +
            "\ntorso: " + torso.ToString() +
            "\nleft_shoulder_pitch: " + left_shoulder_pitch.ToString() +
            "\nleft_shoulder_roll: " + left_shoulder_roll.ToString() +
            "\nleft_shoulder_yaw: " + left_shoulder_yaw.ToString() +
            "\nleft_elbow: " + left_elbow.ToString() +
            "\nright_shoulder_pitch: " + right_shoulder_pitch.ToString() +
            "\nright_shoulder_roll: " + right_shoulder_roll.ToString() +
            "\nright_shoulder_yaw: " + right_shoulder_yaw.ToString() +
            "\nright_elbow: " + right_elbow.ToString();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize);
        }
    }
}
