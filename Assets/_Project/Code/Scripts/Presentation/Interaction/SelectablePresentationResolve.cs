using Core.Entity;
using UnityEngine;

namespace Gameplay.Presentation.Interaction
{
    /// <summary>从碰撞体解析「可选中单位根」：优先带 <see cref="RimOutlineDriver"/> 的根，否则回退 <see cref="EntityBase"/>。</summary>
    public static class SelectablePresentationResolve
    {
        public static Transform TryResolveRoot(Collider collider)
        {
            if (collider == null)
                return null;

            var rim = collider.GetComponentInParent<RimOutlineDriver>();
            if (rim != null)
                return rim.PresentationRoot;

            var entity = collider.GetComponentInParent<EntityBase>();
            return entity != null ? entity.transform : null;
        }
    }
}
