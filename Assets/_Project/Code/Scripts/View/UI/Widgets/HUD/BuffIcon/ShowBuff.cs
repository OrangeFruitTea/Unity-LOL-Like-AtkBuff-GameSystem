using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ShowBuff : MonoBehaviour
{
    [SerializeField, Header("BuffUI预制体")] private GameObject _buffItemTemplate;
    [SerializeField, Header("对象池")] private GameObject _buffPool;
    [SerializeField, Header("BuffUI父物体")] private GameObject _buffs;
    [SerializeField, Header("与buff相关联的游戏对象")] private GameObject _player;
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
