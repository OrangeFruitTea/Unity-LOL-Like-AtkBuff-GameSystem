using Core.ECS;
using Core.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Widgets.PlayerStatement
{
    public class SliderBarController : MonoBehaviour
    {
        [Header("EntityBridge")]
        public EcsEntityBridge ecsBridge;

        [Tooltip("勾选后绑定 CrtMp / MpLimit（典型为 MainStatement 上的第二条 Slider）。")]
        [SerializeField]
        private bool bindManaBars;

        [Header("Components")]
        public Slider slider;
        public TextMeshProUGUI valueInfo;
        public Image fillImage;

        [Header("Values")]
        public Color fillColor = Color.white;

        private EntityDataComponent _dataComponent;
        private bool _wired;

        /// <summary>由 <see cref="StatementWidget"/> / 生成流程注入桥接；也可在 Inspector 预填后在 <see cref="Start"/> 热身。</summary>
        public void SetEntityBridge(EcsEntityBridge bridge)
        {
            ecsBridge = bridge;
            TryWarmRef();
        }

        private void Start()
        {
            TryWarmRef();
        }

        private void TryWarmRef()
        {
            if (ecsBridge == null || !ecsBridge.IsValid())
                return;

            _dataComponent = ecsBridge.GetComponent<EntityDataComponent>();
            if (slider == null || fillImage == null)
            {
                Debug.LogWarning($"{nameof(SliderBarController)} on {name}: Slider/Fill missing.");
                enabled = false;
                return;
            }

            SetFillColor();
            _wired = true;
        }

        private void Update()
        {
            if (!_wired)
                return;

            UpdateValueBar();
        }

        private void UpdateValueBar()
        {
            double currentValue;
            double maxValue;
            if (bindManaBars)
            {
                currentValue = _dataComponent.GetData(EntityBaseDataCore.CrtMp);
                maxValue = _dataComponent.GetData(EntityBaseDataCore.MpLimit);
            }
            else
            {
                currentValue = _dataComponent.GetData(EntityBaseDataCore.CrtHp);
                maxValue = _dataComponent.GetData(EntityBaseDataCore.HpLimit);
            }

            if (maxValue <= 0)
                return;

            slider.normalizedValue = (float)(currentValue / maxValue);
            if (valueInfo != null)
                valueInfo.text = $"{(int)currentValue}/{(int)maxValue}";
        }

        private void SetFillColor()
        {
            if (fillImage != null)
                fillImage.color = fillColor;
        }
    }
}
