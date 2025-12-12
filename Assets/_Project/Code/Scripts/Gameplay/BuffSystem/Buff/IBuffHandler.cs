using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBuffHandler
{
    void Init(object[] args);
    void Reset();
    void HandleBuff(BuffBase buff);
    void OnGet();
    void OnLost();
    void OnLevelChange(uint change);
}
