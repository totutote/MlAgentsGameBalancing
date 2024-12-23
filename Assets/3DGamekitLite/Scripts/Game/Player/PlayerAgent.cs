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
    private Transform movingPlatform;

    [SerializeField]
    private EnemyController[] enemyControllers;

    [SerializeField]
    private Damageable[] breakableBoxes;

    private bool[] switchStatus = new bool[10];

    private bool isEndEpisode = false;

    private bool isFireButtonPressed = false;

    private int restartCount = 0;

    void Update()
    {
        // ボタンが押された瞬間を検出し、フラグを立てる
        if (Input.GetButtonDown("Fire1"))
        {
            isFireButtonPressed = true;
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
        restartCount = 0;
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

        if (restartCount++ > 3000)
        {
            OnEndEpisode();
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        playerInput.MoveInput = new Vector2(actionBuffers.ContinuousActions[0], actionBuffers.ContinuousActions[1]);
        playerInput.AttackInput = actionBuffers.DiscreteActions[0] == 1;
        playerInput.JumpInput = actionBuffers.DiscreteActions[1] == 1;
        if (actionBuffers.DiscreteActions[0] == 1)
        {
            isFireButtonPressed = false;
        }

        //AddReward(-0.0001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Debug.Log("Heuristic method called");

        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");

        var discreteActionsOut = actionsOut.DiscreteActions;

        // フラグの状態を使用して処理を行う
        if (isFireButtonPressed)
        {
            discreteActionsOut[0] = 1;
            // フラグをリセット
            isFireButtonPressed = false;
        }

        // discreteActionsOut[0] = Input.GetButtonDown("Fire1") ? 1 : 0;
        discreteActionsOut[1] = Input.GetButton("Jump") ? 1 : 0;
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
        restartCount = 0;
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