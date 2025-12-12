using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BuffMetadata
{
    public string name;
    public string desc;
    public string iconPath;

    public BuffMetadata(
        string name = "Default Name", 
        string desc = "Default Description", 
        string iconPath = "None")
    {
        this.name = name;
        this.desc = desc;
        this.iconPath = iconPath;
    }
}
