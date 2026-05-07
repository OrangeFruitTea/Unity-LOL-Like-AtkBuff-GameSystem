using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.Presentation.Interaction
{
    /// <summary>点地提示与 <see cref="UnityEngine.AI.NavMeshAgent"/> 对齐时的 NavMesh 投影（设计文档 §7）。</summary>
    public static class NavMeshPointPresentation
    {
        public static bool TryProjectOntoWalkable(Vector3 probe, float sampleMaxDistance, int areaMask, out Vector3 snapped)
        {
            snapped = probe;
            if (NavMesh.SamplePosition(probe, out var hit, sampleMaxDistance, areaMask))
            {
                snapped = hit.position;
                return true;
            }

            return false;
        }
    }
}
