using Gameplay.Presentation.Interaction;
using UnityEngine;

namespace Core.Entity
{
    /// <summary>
    /// 玩家在由远及近靠近防御塔时，按距离渐显世界 UI 攻击范围（透明→不透明）。<br/>
    /// 需同实体（或子级）有 <see cref="AttackRangeWorldUiDiscPresenter"/>（射程与碟大小在 Presenter Inspector 配置）；塔根带 <see cref="EntityBase"/> 供定位。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(25)]
    public sealed class TowerAttackRangeProximityUiReveal : MonoBehaviour
    {
        [SerializeField]
        private EntityBase towerEntity;

        [SerializeField]
        private AttackRangeWorldUiDiscPresenter rangePresenter;

        [Tooltip("留空则尝试 TestPlayerSpawner.LastSpawnedPlayerRoot，其次 Main Camera")]
        [SerializeField]
        private Transform viewerTransform;

        [Tooltip("距离小于等于该值（相对塔）：范围图透明度视为 100%（仍会乘美术在 Image/CG 上设的 Alpha）")]
        [SerializeField]
        [Min(0f)]
        private float fullyVisibleWithinWorldDistance = 420f;

        [Tooltip("距离大于等于该值：完全不显示")]
        [SerializeField]
        [Min(0f)]
        private float invisibleBeyondWorldDistance = 600f;

        [Tooltip("true：仅用 XZ 距离；false：三维距离")]
        [SerializeField]
        private bool useHorizontalDistance = true;

        private void Awake()
        {
            if (towerEntity == null)
                towerEntity = GetComponent<EntityBase>() ?? GetComponentInParent<EntityBase>();

            if (rangePresenter == null)
                rangePresenter = GetComponentInChildren<AttackRangeWorldUiDiscPresenter>(true);
        }

        private void OnDisable()
        {
            if (rangePresenter != null)
                rangePresenter.SetVisible(false);
        }

        private void LateUpdate()
        {
            if (rangePresenter == null || towerEntity == null)
                return;

            var viewer = ResolveViewer();
            if (viewer == null)
            {
                rangePresenter.SetVisible(false);
                return;
            }

            if (invisibleBeyondWorldDistance <= fullyVisibleWithinWorldDistance + 1e-3f)
            {
                rangePresenter.SetVisible(false);
                return;
            }

            Vector3 tp = towerEntity.transform.position;
            Vector3 vp = viewer.position;

            float d = useHorizontalDistance
                ? Vector2.Distance(new Vector2(tp.x, tp.z), new Vector2(vp.x, vp.z))
                : Vector3.Distance(tp, vp);

            if (d >= invisibleBeyondWorldDistance)
            {
                rangePresenter.SetVisible(false);
                return;
            }

            float alpha =
                d <= fullyVisibleWithinWorldDistance
                    ? 1f
                    : 1f - (d - fullyVisibleWithinWorldDistance) /
                        Mathf.Max(1e-3f, invisibleBeyondWorldDistance - fullyVisibleWithinWorldDistance);

            // 范围碟直径在 AttackRangeWorldUiDiscPresenter Inspector 中用 attackRadiusBaseline 配置
            rangePresenter.SetWorldDisc(tp, alpha);
        }

        private Transform ResolveViewer()
        {
            if (viewerTransform != null)
                return viewerTransform;
            if (TestPlayerSpawner.LastSpawnedPlayerRoot != null)
                return TestPlayerSpawner.LastSpawnedPlayerRoot;
            return Camera.main != null ? Camera.main.transform : null;
        }
    }
}
