using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Mujoco;

public class ControlAgent_Unitreeh1 : Agent
{
    public Transform head_target;
    public Transform head_self;
    public float force_multiplier = 1000;

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
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(head_target.position - head_self.position);
    }


    public override void OnEpisodeBegin()
    {
        head_target.position = new Vector3(Random.Range(-4, 4), 1.7f, Random.Range(-4, 4));
    }

    //public override void Heuristic(in ActionBuffers actionsOut)
    //{

    //}

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {

        left_hip_yaw.Control = actionBuffers.ContinuousActions[0] * force_multiplier;
        left_hip_roll.Control = actionBuffers.ContinuousActions[1] * force_multiplier;
        left_hip_pitch.Control = actionBuffers.ContinuousActions[2] * force_multiplier;
        left_knee.Control = actionBuffers.ContinuousActions[3] * force_multiplier;
        left_ankle.Control = actionBuffers.ContinuousActions[4] * force_multiplier;
        right_hip_yaw.Control = actionBuffers.ContinuousActions[5] * force_multiplier;
        right_hip_roll.Control = actionBuffers.ContinuousActions[6] * force_multiplier;
        right_hip_pitch.Control = actionBuffers.ContinuousActions[7] * force_multiplier;
        right_knee.Control = actionBuffers.ContinuousActions[8] * force_multiplier;
        right_ankle.Control = actionBuffers.ContinuousActions[9] * force_multiplier;
        torso.Control = actionBuffers.ContinuousActions[10] * force_multiplier;
        left_shoulder_pitch.Control = actionBuffers.ContinuousActions[11] * force_multiplier;
        left_shoulder_roll.Control = actionBuffers.ContinuousActions[12] * force_multiplier;
        left_shoulder_yaw.Control = actionBuffers.ContinuousActions[13] * force_multiplier;
        left_elbow.Control = actionBuffers.ContinuousActions[14] * force_multiplier;
        right_shoulder_pitch.Control = actionBuffers.ContinuousActions[15] * force_multiplier;
        right_shoulder_roll.Control = actionBuffers.ContinuousActions[16] * force_multiplier;
        right_shoulder_yaw.Control = actionBuffers.ContinuousActions[17] * force_multiplier;
        right_elbow.Control = actionBuffers.ContinuousActions[18] * force_multiplier;

        Vector3 error = head_target.position - head_self.position;
        if (error.magnitude < 1)
        {
            SetReward(1f);
            EndEpisode();
        }

        if (StepCount > 1000)
        {
            EndEpisode();
        }
    }
}
