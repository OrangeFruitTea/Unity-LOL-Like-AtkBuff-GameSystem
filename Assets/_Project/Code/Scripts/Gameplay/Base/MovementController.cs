using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Tooltip(
        "用于将屏幕射线落到「地表」：未勾选的层不参与 Physics 射线。\n行走面须有 Collider（如 MeshCollider），否则射线会穿透，只能偶尔点到自身或其它物体的碰撞盒。")]
    [SerializeField] private LayerMask clickRaycastMask = Physics.DefaultRaycastLayers;

    [Tooltip("若为 true：按距离跳过命中的碰撞体中与自身同一单位（本 Transform 及以下）的子碰撞体，避免点到身上导致仅能小范围挪动。")]
    [SerializeField] private bool skipOwnCollidersWhenRaycasting = true;

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
            foreach (var hit in GetOrderedRayHits(ray))
            {
                if (skipOwnCollidersWhenRaycasting &&
                    hit.collider != null &&
                    IsColliderBelowOrOn(hit.collider.transform, transform))
                    continue;

                if (!NavMesh.SamplePosition(hit.point, out var navHit, navMeshSampleRadius, NavMesh.AllAreas))
                    continue;

                ApplyMove(navHit.position);
                return;
            }

            Debug.LogWarning(
                $"{nameof(MovementController)}: 从屏幕射线未得到可用落点——请确认地面有可射线检测的 Collider，且图层在 clickRaycastMask 内。"
                + $"（若在 {navMeshSampleRadius}m 内均无 NavMesh 也会跳过该次命中）。");
        }
    }

    private IEnumerable<RaycastHit> GetOrderedRayHits(Ray ray)
    {
        var hits = Physics.RaycastAll(ray, Mathf.Infinity, clickRaycastMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            yield break;

        foreach (var h in hits.OrderBy(x => x.distance))
            yield return h;
    }

    private static bool IsColliderBelowOrOn(Transform hitTransform, Transform selfRoot)
    {
        while (hitTransform != null)
        {
            if (hitTransform == selfRoot)
                return true;
            hitTransform = hitTransform.parent;
        }

        return false;
    }

    private void ApplyMove(Vector3 destinationOnNavmesh)
    {
        _agent.SetDestination(destinationOnNavmesh);

        var look = destinationOnNavmesh - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude < 0.0001f)
            return;

        var rotationToLookAt = Quaternion.LookRotation(look);
        var rotationY = Mathf.SmoothDampAngle(
            transform.eulerAngles.y,
            rotationToLookAt.eulerAngles.y,
            ref _rotateVelocity,
            rotateSpeedMovement * (Time.deltaTime * 5));
        transform.eulerAngles = new Vector3(0, rotationY, 0);
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
