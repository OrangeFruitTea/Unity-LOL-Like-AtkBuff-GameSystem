using System.Collections;
using System.Collections.Generic;
using Core.Entity;
using UnityEngine;

public class BuffRuntimeData
{
    public EntityBase Provider { get; set; }
    public EntityBase Owner { get; set; }
    public uint CurrentLevel { get; set; }
    public float ResidualDuration { get; set; }
    public bool IsInitialized { get; set; }

    public void Clear()
    {
        Provider = null;
        Owner = null;
        CurrentLevel = 0;
        ResidualDuration = 0;
        IsInitialized = false;
    }
}