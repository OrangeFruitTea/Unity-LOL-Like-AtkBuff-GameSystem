using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class BuffIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Header("Mask")]
    private Image _mask_M;
    [SerializeField, Header("LevelText")] 
    private TextMeshProUGUI _Level;
    [SerializeField, Header("Frame")] 
    private Image _frame;
    [SerializeField, Header("Icon")] 
    private Image _icon;

    [SerializeField, Header("DetailWindow")]
    private GameObject _buffInfo;
    [SerializeField, Header("BuffNameText")] 
    private TextMeshProUGUI _buffName;
    [SerializeField, Header("BuffDescText")] 
    private TextMeshProUGUI _buffDescription;
    [SerializeField, Header("BuffProviderText")]
    private TextMeshProUGUI _buffProvider;

    private SafeObjectPool<BuffIcon> _recyclePool;

    // 是否已经初始化
    private bool _Initialized = false;
    // 是否需要显示等级
    private bool _ShowLevel = false;
    // 是否显示计时工具
    private bool _ShowClockLine = false;

    private BuffBase _targetBuff;

    private void OnPointerEnter()
    {
        _buffInfo.gameObject.SetActive(true);
        ShowInfo(_targetBuff);
    }

    private void OnPointerExit()
    {
        _buffInfo.gameObject.SetActive(false);
    }

    public void Initialize(BuffBase buff, SafeObjectPool<BuffIcon> recyclePool)
    {
        _icon.sprite = Resources.Load<Sprite>(buff.Metadata.iconPath);
        _targetBuff = buff;
        _recyclePool = recyclePool;
        if (_targetBuff.Config.maxLevel > 1)
        {
            _ShowLevel = true;
            _Level.gameObject.SetActive(true);
        }
        else
        {
            _ShowLevel = false;
            _Level.gameObject.SetActive(false);
        }

        switch (buff.Config.type)
        {
            case BuffType.Buff:
                _frame.color = Color.green;
                break;
            case BuffType.Debuff:
                _frame.color = Color.red;
                break;
            case BuffType.None:
                _frame.color = Color.white;
                break;
            default:
                break;
        }
        _Initialized = true;
    }
    // 显示buff详细信息
    public void ShowInfo(BuffBase buff)
    {
        _buffName.text = buff.Metadata.name;
        _buffDescription.text = buff.Metadata.desc;
        _buffProvider.text = "来源：" + buff.ProviderName;
    }

    private void Update()
    {
        if (_Initialized)
        {
            // 计时工具显示
            if (_ShowClockLine)
            {
                _mask_M.fillAmount = 1 - (_targetBuff.RuntimeData.ResidualDuration / _targetBuff.Config.maxDuration);
            }
            // 等级显示
            if (_ShowLevel)
            {
                _Level.text = _targetBuff.RuntimeData.CurrentLevel.ToString();
            }
            // 如果等级归零则回收
            if (_targetBuff.RuntimeData.CurrentLevel == 0)
            {
                _recyclePool.Release(this);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnPointerEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnPointerExit();
    }
}
