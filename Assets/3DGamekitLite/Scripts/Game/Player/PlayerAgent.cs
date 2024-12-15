using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Gamekit3D;

public class PlayerAgent : Agent
{
    

    public override void Initialize()
    {
        Debug.Log("Initialize called");
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin called");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("CollectObservations called");
        sensor.AddObservation(transform.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        Debug.Log("OnActionReceived called");

        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];
        controlSignal.z = actionBuffers.ContinuousActions[1];

        if (actionBuffers.DiscreteActions[1] == 1)
        {
            EndEpisode();
            SceneController.RestartZone();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("Heuristic method called");

        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");

        Debug.Log(continuousActionsOut[0]);

        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = Input.GetMouseButtonDown(0) ? 1 : 0;
        discreteActionsOut[1] = Input.GetKeyDown(KeyCode.Escape) ? 1 : 0;
    }
}