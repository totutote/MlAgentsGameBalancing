using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Gamekit3D;

public class PlayerAgent : Agent
{
    [SerializeField]
    private PlayerInput playerInput;

    [SerializeField]
    private Transform sensorCamera;

    [SerializeField]
    private Transform movingPlatform;

    [SerializeField]
    private EnemyController[] enemyControllers;

    [SerializeField]
    private Damageable[] breakableBoxes;

    private bool[] switchStatus = new bool[10];

    private bool isEndEpisode = false;

    private bool isFireButtonPressed = false;

    void Update()
    {
        if (StepCount > 5000)
        {
            OnEndEpisode();
        }

        // ボタンが押された瞬間を検出し、フラグを立てる
        if (Input.GetButtonDown("Fire1"))
        {
            isFireButtonPressed = true;
        }

        if (sensorCamera != null)
        {
            sensorCamera.rotation = Quaternion.Euler(90, 0, 0);
        }
    }

    public override void Initialize()
    {
        Debug.Log("Initialize called");
        foreach(var breakableBox in breakableBoxes)
        {
            breakableBox.OnDeath.AddListener(OnBoxBroken);
        }
        foreach(var enemyController in enemyControllers)
        {
            enemyController.OnDeath.AddListener(OnEnemyDie);
        }
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin called");
        AudioListener.pause = true;
        isEndEpisode = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Debug.Log("CollectObservations called");
        sensor.AddObservation(transform.position / 100f);
        if (movingPlatform == null)
        {
            sensor.AddObservation(Vector3.zero);
        }
        else
        {
            sensor.AddObservation(movingPlatform.position / 100f);
        }

        foreach (var enemyController in enemyControllers)
        {
            if (enemyController == null)
            {
                sensor.AddObservation(Vector3.zero);
                continue;
            }
            sensor.AddObservation(enemyController.transform.position / 100f);
        }

        foreach (var breakableBox in breakableBoxes)
        {
            if (breakableBox == null || breakableBox.gameObject.activeSelf == false)
            {
                sensor.AddObservation(Vector3.zero);
                continue;
            }
            sensor.AddObservation(breakableBox.transform.position / 100f);
        }
        
        foreach (var status in switchStatus)
        {
            sensor.AddObservation(status);
        }

        //sensor.AddObservation(playerInput.playerControllerInputBlocked);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // playerInput.MoveInput = new Vector2(actionBuffers.ContinuousActions[0], actionBuffers.ContinuousActions[1]);
        playerInput.MoveInput = new Vector2(
            actionBuffers.DiscreteActions[0] == 4 ? 1 : actionBuffers.DiscreteActions[0] == 3 ? -1 : 0,
            actionBuffers.DiscreteActions[0] == 1 ? 1 : actionBuffers.DiscreteActions[0] == 2 ? -1 : 0
        );
        playerInput.AttackInput = actionBuffers.DiscreteActions[1] == 1;
        if (actionBuffers.DiscreteActions[1] == 1)
        {
            isFireButtonPressed = false;
        }

        playerInput.JumpInput = actionBuffers.DiscreteActions[2] == 1;

        //AddReward(-0.0001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Debug.Log("Heuristic method called");

        var discreteActionsOut = actionsOut.DiscreteActions;

        // discreteActionsOut[0]を0から4に設定
        if (Input.GetAxis("Horizontal") > 0)
            discreteActionsOut[0] = 4;
        else if (Input.GetAxis("Horizontal") < 0)
            discreteActionsOut[0] = 3;
        else if (Input.GetAxis("Vertical") > 0)
            discreteActionsOut[0] = 1;
        else if (Input.GetAxis("Vertical") < 0)
            discreteActionsOut[0] = 2;
        else
            discreteActionsOut[0] = 0;

        if (isFireButtonPressed)
        {
            discreteActionsOut[1] = 1;
            // フラグをリセット
            isFireButtonPressed = false;
        }

        // discreteActionsOut[0] = Input.GetButtonDown("Fire1") ? 1 : 0;
        discreteActionsOut[2] = Input.GetButton("Jump") ? 1 : 0;
    }

    private void OnEndEpisode()
    {
        if (isEndEpisode)
        {
            return;
        }
        isEndEpisode = true;
        EndEpisode();
        if (SceneController.Transitioning)
        {
            return;
        }
        SceneController.RestartZone();
    }

    public void OnDie()
    {
        SetReward(-0.01f);
        OnEndEpisode();
    }

    public void OnDamage()
    {
        AddReward(-0.01f);
    }

    public void OnGoal()
    {
        AddReward(1.0f);
        OnEndEpisode();
    }

    public void OnEnterTrigger(int index)
    {
        if (switchStatus[index] == false)
        {
            AddReward(0.5f);
        }
        switchStatus[index] = true;
    }

    public void OnBoxBroken()
    {
        AddReward(0.5f);
    }

    public void OnEnemyDie()
    {
        AddReward(0.5f);
    }
}