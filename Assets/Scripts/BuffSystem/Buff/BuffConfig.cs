using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BuffConfig
{
    public int id;
    public BuffType type;
    public BuffConflictResolution resolution;
    public float maxDuration;
    public float frequency;
    public uint maxLevel;
    public uint demotion;
    public bool dispellable;

    public BuffConfig(
        BuffType type = BuffType.None, 
        BuffConflictResolution resolution = BuffConflictResolution.Cover, 
        float maxDuration = 5.0f, 
        float frequency = 1.0f, 
        uint maxLevel = 5, 
        uint demotion = 1, 
        bool dispellable = true)
    {
        this.type = type;
        this.resolution = resolution;
        this.maxDuration = maxDuration;
        this.frequency = frequency;
        this.maxLevel = maxLevel;
        this.demotion = demotion;
        this.dispellable = dispellable;
    }
}
