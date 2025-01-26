using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Gamekit3D;
using System.Collections.Generic;
using System.Collections;

public class PlayerAgent : Agent
{
    [SerializeField]
    private PlayerInput playerInput;

    [SerializeField]
    private Transform sensorCamera;

    [SerializeField]
    private Transform RaySensor;

    [SerializeField]
    private Transform movingPlatform;

    [SerializeField]
    private EnemyController[] enemyControllers;

    [SerializeField]
    private Damageable[] breakableBoxes;

    [SerializeField]
    private GameObject[] switches;

    [SerializeField]
    private float rewardDistanceMoved = 0.001f;

    [SerializeField]
    private float rewardOnDie = -1.0f;

    [SerializeField]
    private float rewardOnDamage = -0.01f;

    [SerializeField]
    private float rewardOnGoal = 1.0f;

    [SerializeField]
    private float rewardOnEnterTrigger = 5.0f;

    [SerializeField]
    private float rewardOnBoxBroken = 1.0f;

    [SerializeField]
    private float rewardOnEnemyDie = 1.0f;

    [SerializeField]
    private List<Vector3> startPositions;

    private bool[] switchStatus = new bool[10];

    private bool isEndEpisode = false;

    private bool isFireButtonPressed = false;

    private Vector3 previousPosition; // 追加

    void Update()
    {
        if (StepCount > 10000)
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

        if (RaySensor != null)
        {
            //RaySensor.position = new Vector3(RaySensor.position.x, 0.0f, RaySensor.position.z);
            RaySensor.rotation = Quaternion.Euler(0, 0, 0);
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
        
        // スタート位置をランダムに設定
        if (startPositions != null && startPositions.Count > 0)
        {
            Vector3 startPos = startPositions[Random.Range(0, startPositions.Count)];
            StartCoroutine(DelayedSetPosition(startPos));
        }
    }

    private IEnumerator DelayedSetPosition(Vector3 startPos)
    {
        // 数フレーム遅延させる
        for (int i = 0; i < 2; i++)
        {
            yield return null;
        }
        playerInput.transform.localPosition = startPos;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Debug.Log("CollectObservations called");
        // sensor.AddObservation(transform.position / 100f);
        if (movingPlatform == null)
        {
            sensor.AddObservation(Vector3.zero);
        }
        else
        {
            sensor.AddObservation(movingPlatform.position / 100f);
        }

/*
        foreach (var enemyController in enemyControllers)
        {
            if (enemyController == null)
            {
                sensor.AddObservation(Vector3.zero);
                continue;
            }
            sensor.AddObservation(enemyController.transform.position / 100f);
        }
*/

        foreach (var breakableBox in breakableBoxes)
        {
            if (breakableBox == null || breakableBox.gameObject.activeInHierarchy == false)
            {
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(0f);
                continue;
            }
            // プレイヤーとボックスの方向と距離を計算して観測に追加
            Vector3 direction = (breakableBox.transform.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, breakableBox.transform.position);
            sensor.AddObservation(direction);
            sensor.AddObservation(distance / 100f); // 距離をスケール
        }

        // switchesの方向と距離を計算して観測に追加
        foreach (var switchObj in switches)
        {
            if (switchObj == null || !switchObj.activeInHierarchy)
            {
                // 無効値を投入
                sensor.AddObservation(Vector3.zero);
                sensor.AddObservation(0f);
                continue;
            }
            Vector3 direction = (switchObj.transform.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, switchObj.transform.position);
            sensor.AddObservation(direction);
            sensor.AddObservation(distance / 100f); // 距離をスケール
        }
        
        /*
        foreach (var status in switchStatus)
        {
            sensor.AddObservation(status);
        }
        */

        sensor.AddObservation(playerInput.HaveControl());

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

        // XZ平面上の移動距離を計算
        Vector2 currentPosition = new Vector2(transform.position.x, transform.position.z);
        Vector2 prevPosition = new Vector2(previousPosition.x, previousPosition.z);
        float distanceMoved = Vector2.Distance(currentPosition, prevPosition);
        // 移動距離に基づいて報酬を追加
        AddReward(distanceMoved * rewardDistanceMoved); // 報酬係数は調整可能
        // 前回の位置を更新
        previousPosition = transform.position;

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
        //EndEpisode();
        if (SceneController.Transitioning)
        {
            return;
        }
        SceneController.RestartZone();
    }

    public void OnDie()
    {
        AddReward(rewardOnDie);
        OnEndEpisode();
    }

    public void OnDamage()
    {
        AddReward(rewardOnDamage);
    }

    public void OnGoal()
    {
        AddReward(rewardOnGoal);
        OnEndEpisode();
    }

    public void OnEnterTrigger(int index)
    {
        if (switchStatus[index] == false)
        {
            AddReward(rewardOnEnterTrigger);
        }
        switchStatus[index] = true;
    }

    public void OnBoxBroken()
    {
        AddReward(rewardOnBoxBroken);
    }

    public void OnEnemyDie()
    {
        AddReward(rewardOnEnemyDie);
    }
}