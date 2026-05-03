using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    [Tooltip("关闭后本物体不再监听点击寻路；用于木桩/AI 单位等与玩家共用 Prefab 时避免和主控一起 SetDestination。")]
    [SerializeField] private bool reactToPlayerMoveInput = true;

    private NavMeshAgent _agent;
    public float rotateSpeedMovement = 0.1f;
    private float _rotateVelocity;
    
    public float speed = 5.0f;
    public KeyCode moveKey = KeyCode.Mouse1;
    private CharacterController _controller;

    [Tooltip("将自身 / 点击点投影到 NavMesh 时的最大水平搜索半径（米）。")]
    [SerializeField] private float navMeshSampleRadius = 4f;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!reactToPlayerMoveInput)
            return;

        if (_agent == null || !_agent.isActiveAndEnabled)
            return;

        if (Input.GetKeyDown(moveKey))
        {
            if (!TryEnsureAgentOnNavMesh())
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // 确认是否点击到使用NavMeshSystem的物体
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                if (!NavMesh.SamplePosition(hit.point, out var navHit, navMeshSampleRadius, NavMesh.AllAreas))
                {
                    Debug.LogWarning(
                        $"{nameof(MovementController)}: 点击处附近 {navMeshSampleRadius}m 内无 NavMesh，已忽略本次移动。");
                    return;
                }

                // MOVEMENT（目标点必须为可导航点）
                _agent.SetDestination(navHit.position);
                // ROTATION
                var look = navHit.position - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude < 0.0001f)
                    return;

                Quaternion rotationToLookAt = Quaternion.LookRotation(look);
                float rotationY = Mathf.SmoothDampAngle(transform.eulerAngles.y,
                    rotationToLookAt.eulerAngles.y,
                    ref _rotateVelocity,
                    rotateSpeedMovement * (Time.deltaTime * 5));
                transform.eulerAngles = new Vector3(0, rotationY, 0);
            }
        }
    }

    /// <summary>
    /// Agent 未落在 NavMesh 上时（例如生成在悬空/离线位置），尝试投影后 Warp，否则 SetDestination 会报错。
    /// </summary>
    private bool TryEnsureAgentOnNavMesh()
    {
        if (_agent.isOnNavMesh)
            return true;

        if (NavMesh.SamplePosition(transform.position, out var hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            _agent.Warp(hit.position);
            return _agent.isOnNavMesh;
        }

        Debug.LogWarning(
            $"{nameof(MovementController)} on {name}: 单位附近 {navMeshSampleRadius}m 内无 NavMesh，无法寻路。" +
            "请检查 NavMesh 烘焙、单位高度是否在网格上，或调大 navMeshSampleRadius。");
        return false;
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
        _controller.enabled = true;
    }
}
