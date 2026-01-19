using System.Collections;
using Core.ECS;
using UnityEngine;

namespace Core.Entity
{
    public class TestPlayer : EntityBase
    {
        
    }
    public class TestPlayerSpawner : EntityBase
    {
        [SerializeField] private EntityBase testPlayerPrefab;
        private bool _isTestPlayerPrefabNull;

        private void Start()
        {
            _isTestPlayerPrefabNull = testPlayerPrefab == null;
            StartCoroutine(SpawnTestPlayer());
        }

        private IEnumerator SpawnTestPlayer()
        {
            yield return null;
            if (_isTestPlayerPrefabNull)
            {
                Debug.LogError("需要在Inspector中赋值TestPlayer预制体");
                yield break;
            }

            var spawnSystem = EcsWorld.Instance.GetEcsSystem<EntitySpawnSystem>();
            if (spawnSystem == null)
            {
                Debug.LogError("EntitySpawnSystem未注册");
                yield break;
            }
            spawnSystem.AddPendingEntity(testPlayerPrefab);
            Debug.Log("TestPlayer已加入生成队列");
        }
    }
}
