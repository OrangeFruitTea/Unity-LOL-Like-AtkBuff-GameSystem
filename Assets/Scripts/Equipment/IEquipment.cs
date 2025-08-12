using System.Collections;
using System.Collections.Generic;
using Core.Entity;
using UnityEngine;

public class EquipmentMetaData
{
    private string Name { get; }
    private string Description { get; }
    private int Price { get; }
    
}

public interface IEquipment
{
    double GetProvidedValue(EntityBaseData data);
    double GetProvidedValue(EntityBaseDataCore dataCore);
    bool HasActiveEffect();
    
}
