using Core.ECS;
using Core.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Widgets.PlayerStatement
{
    public class SliderBarController : MonoBehaviour
    {
        [Header("EntityBridge")] public EcsEntityBridge ecsBridge;
        [Header("Components")]
        public Slider slider;
        public TextMeshProUGUI valueInfo;
        public Image fillImage;
        [Header("Values")]
        public Color fillColor = Color.white;

        private EntityDataComponent _dataComponent;

        // Start is called before the first frame update
        void Start()
        {
            if (!ecsBridge.IsValid())
            {
                Debug.LogError("SliderBarController初始化失败：实体或管理器为空");
                enabled = false;
                return;
            }

            _dataComponent = ecsBridge.GetComponent<EntityDataComponent>();
            SetFillColor();
        }

        public void TakeDamage(float value)
        {
        }

        public void Heal(float value)
        {
        }
        private void UpdateValueBar()
        {
            var currentValue = _dataComponent.GetData(EntityBaseDataCore.CrtHp);
            var maxValue = _dataComponent.GetData(EntityBaseDataCore.HpLimit);
            if (maxValue <= 0) return;
            slider.value = (float)(currentValue / maxValue);
            valueInfo.text = $"{(int)currentValue / (int)maxValue }";
        }

        private void SetFillColor()
        {
            fillImage.color = fillColor;
        }
    }
}
