using System.Linq;
using Core.ECS;
using Core.Entity;
using Core.UI;
using TMPro;
using UnityEngine;

namespace Widgets.PlayerStatement
{
    public class DetailStatItemManager : MonoBehaviour, IEntityBridgeBindable
    {

        #region staticEnum

        public static int AtkAD = 0;
        public static int AtkAP = 1;
        public static int DefAD = 2;
        public static int DefAP = 3;
        public static int AtkSpeed = 4;
        public static int SkillCd = 5;
        public static int CriticalRate = 6;
        public static int MoveSpeed= 7;

        #endregion
        public GameObject[] statItems;
        public EcsEntityBridge ecsBridge;

        private bool IsStatLayoutReady() =>
            statItems != null
            && statItems.Length > MoveSpeed
            && statItems.All(obj => obj != null && obj.GetComponent<DetailStatItem>() != null);

        /// <inheritdoc />
        public void Bind(EcsEntityBridge bridge)
        {
            ecsBridge = bridge;
            RefreshFromBridge();
        }

        private void Start()
        {
            if (IsStatLayoutReady() && ecsBridge != null && ecsBridge.IsValid())
                RefreshFromBridge();
        }

        /// <summary> 从当前 <see cref="ecsBridge"/> 刷新数值（Inspector 预绑或 <see cref="Bind"/> 后调用）。 </summary>
        public void RefreshFromBridge()
        {
            if (!IsStatLayoutReady())
                return;
            if (ecsBridge == null || !ecsBridge.IsValid())
                return;

            var dataComponent = ecsBridge.GetComponent<EntityDataComponent>();
            SetValue(AtkAD, dataComponent.GetData(EntityBaseDataCore.AtkAD));
            SetValue(AtkAP, dataComponent.GetData(EntityBaseDataCore.AtkAP));
            SetValue(DefAD, dataComponent.GetData(EntityBaseDataCore.DefenceAD));
            SetValue(DefAP, dataComponent.GetData(EntityBaseDataCore.DefenceAP));
            SetValue(AtkSpeed, dataComponent.GetData(EntityBaseDataCore.AtkSpeed));
            SetValue(SkillCd, dataComponent.GetData(EntityBaseDataCore.SkillCd));
            SetValue(CriticalRate, dataComponent.GetData(EntityBaseDataCore.CriticalRate));
            SetValue(MoveSpeed, dataComponent.GetData(EntityBaseDataCore.MoveSpeed));
        }

        public void SetValue(int type, double value)
        {
            string str = value.ToString("G");
            if (type == CriticalRate)
            {
                str += "%";
            }

            var text = statItems[type].GetComponentInChildren<TMP_Text>();
            if (text == null) throw Error.ComponentNotFoundException;
            text.text = str;
        }
    }
}
