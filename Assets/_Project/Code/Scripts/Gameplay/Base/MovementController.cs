using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    private NavMeshAgent _agent;
    public float rotateSpeedMovement = 0.1f;
    private float _rotateVelocity;
    
    public float speed = 5.0f;
    public KeyCode moveKey = KeyCode.Mouse1;
    private CharacterController _controller;
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(moveKey))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // 确认是否点击到使用NavMeshSystem的物体
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                // MOVEMENT
                _agent.SetDestination(hit.point);
                // ROTATION
                Quaternion rotationToLookAt = Quaternion.LookRotation(hit.point - transform.position);
                float rotationY = Mathf.SmoothDampAngle(transform.eulerAngles.y,
                    rotationToLookAt.eulerAngles.y,
                    ref _rotateVelocity,
                    rotateSpeedMovement * (Time.deltaTime * 5));
                transform.eulerAngles = new Vector3(0, rotationY, 0);
            }
        }
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
