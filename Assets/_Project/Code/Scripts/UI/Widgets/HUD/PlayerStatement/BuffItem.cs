using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BuffItem : MonoBehaviour
{
    private RawImage _buffIcon;
    private Image _mask;
    // private TMP_Text _levelText;
    private TextMeshProUGUI _levelText;

    private GameObject _buffInfo;
    private TextMeshProUGUI _buffName;
    private TextMeshProUGUI _buffDesc;
    private TextMeshProUGUI _buffProvider;

    private bool _initialized = false;
    private bool _needNumber = false;
    private bool _needLine = false;

    private BuffBase _targetBuff;

    private bool IsValid()
    {
        return (_buffIcon != null && _levelText != null && _mask != null);
    }

    private void Start()
    {
        _buffIcon = GetComponentInChildren<RawImage>();
        _mask = GetComponentInChildren<Image>();
        _levelText = GetComponentInChildren<TextMeshProUGUI>();
        if (!IsValid()) throw Error.WidgetBoundErrorException;
    }

    public void SetLevel(int value)
    {
        _levelText.text = value.ToString("G");
    }
}
