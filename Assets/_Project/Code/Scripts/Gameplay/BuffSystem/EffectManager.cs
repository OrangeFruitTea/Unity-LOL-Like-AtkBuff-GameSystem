using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    #region Singleton
    private static EffectManager _instance;
    public static EffectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var gameObjectInstance = new GameObject("Effect Manager");
                _instance = gameObjectInstance.AddComponent<EffectManager>();
                DontDestroyOnLoad(gameObjectInstance);
            }
            return _instance;
        }
    }
    #endregion

}
