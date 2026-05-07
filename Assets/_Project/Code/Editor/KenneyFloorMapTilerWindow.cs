#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Atk.EditorTools
{
    /// <summary>
    /// 在激活场景中于 <c>Map</c>（或自定父节点）下，用 Kenney ScenePack floor 系列 FBX 按网格批量实例化，
    /// 便于快速摆出地面；不涉及运行时逻辑。
    /// </summary>
    public sealed class KenneyFloorMapTilerWindow : EditorWindow
    {
        private const string DefaultKenneyFloorFolder =
            "Assets/_Project/Art/ThirdParty/KenneyModels/ScenePack_01";

        private Transform _parent;
        private DefaultAsset _floorFolderAsset;
        private int _tilesX = 16;
        private int _tilesZ = 16;
        private float _cellSize = 1f;
        private Vector3 _gridOriginOffset = Vector3.zero;
        private bool _includeBalconyFloors = true;
        private bool _clearChildrenBeforeTile = true;
        private VariationMode _variationMode = VariationMode.Random;
        private int _randomSeed = 12345;
        private bool _randomYawSteps90;

        private List<GameObject> _floorMeshes = new();
        private List<bool> _floorMeshEnabled = new();
        private List<float> _floorMeshWeight = new();
        private Vector2 _scroll;

        private enum VariationMode
        {
            Random,
            Cycle,
        }

        [MenuItem("Tools/场景/Kenney 地板网格平铺 (Map)...")]
        public static void Open() => GetWindow<KenneyFloorMapTilerWindow>("Kenney 地板平铺");

        private void OnEnable()
        {
            if (!_floorMeshes.Any())
                AutoPickFromFolder(DefaultKenneyFloorFolder);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "在 Map 下本地 XZ 平铺。勾选左侧决定参与平铺；「权重」为相对比例——随机模式按权重加权抽取，" +
                "循环模式先按权重展开为一轮（与最小权重取比后取整）再依次轮转。\n" +
                "权重 ≤ 0 不参与。Kenney tile 常为 1m，接缝不对可调「格子尺寸」。",
                MessageType.Info);

            DrawParentField();

            _floorFolderAsset = (DefaultAsset)EditorGUILayout.ObjectField(
                "Floor 文件夹 (ScenePack)", _floorFolderAsset, typeof(DefaultAsset), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("从文件夹载入 floor FBX"))
                {
                    var path = _floorFolderAsset != null ? AssetDatabase.GetAssetPath(_floorFolderAsset) : "";
                    AutoPickFromFolder(string.IsNullOrEmpty(path) ? DefaultKenneyFloorFolder : path);
                }

                if (GUILayout.Button("使用默认 ScenePack 路径"))
                    AutoPickFromFolder(DefaultKenneyFloorFolder);
            }

            _includeBalconyFloors = EditorGUILayout.ToggleLeft("包含 balcony-floor*", _includeBalconyFloors);
            _tilesX = Mathf.Max(1, EditorGUILayout.IntField("格子数 X", _tilesX));
            _tilesZ = Mathf.Max(1, EditorGUILayout.IntField("格子数 Z", _tilesZ));
            _cellSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("格子尺寸 (米)", _cellSize));
            _gridOriginOffset = EditorGUILayout.Vector3Field("起点偏移 (Map 本地空间)", _gridOriginOffset);

            using (new EditorGUILayout.HorizontalScope())
            {
                _variationMode = (VariationMode)EditorGUILayout.EnumPopup("每一块变化", _variationMode);
                if (_variationMode == VariationMode.Random)
                {
                    _randomSeed = EditorGUILayout.IntField("随机种子", _randomSeed);
                }
            }

            _randomYawSteps90 = EditorGUILayout.Toggle("随机 Y 旋转 (0°/90°/…)", _randomYawSteps90);

            EditorGUILayout.Space(4);
            _clearChildrenBeforeTile = EditorGUILayout.ToggleLeft(
                "平铺前清空 Map（仅删子物体）", _clearChildrenBeforeTile);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"已载入模型 ({_floorMeshes.Count})：", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("全部启用", GUILayout.Width(72)))
                    SetAllMeshToggles(true);
                if (GUILayout.Button("全部禁用", GUILayout.Width(72)))
                    SetAllMeshToggles(false);
                if (GUILayout.Button("权重全 1", GUILayout.Width(72)))
                    SetAllWeights(1f);
            }

            SyncToggleAndWeightLists();
            var eligibleCount = CountEligibleMeshes();
            EditorGUILayout.LabelField($"参与平铺（勾选且权重>0）: {eligibleCount} 个", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                EditorGUILayout.LabelField("FBX", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("权重", EditorStyles.miniLabel, GUILayout.Width(56));
            }

            for (var i = 0; i < _floorMeshes.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _floorMeshEnabled[i] = GUILayout.Toggle(
                        _floorMeshEnabled[i],
                        GUIContent.none,
                        GUILayout.Width(18));

                    _floorMeshes[i] = (GameObject)EditorGUILayout.ObjectField(
                        _floorMeshes[i],
                        typeof(GameObject),
                        false);

                    _floorMeshWeight[i] = Mathf.Max(
                        0f,
                        EditorGUILayout.FloatField(_floorMeshWeight[i], GUILayout.Width(56)));
                }
            }

            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = true;
                if (GUILayout.Button("仅清空 Map 子物体"))
                {
                    var p = ResolveParent();
                    if (p == null)
                        return;

                    Undo.IncrementCurrentGroup();
                    ClearMapChildren(p);
                    MarkActiveSceneDirty();
                }

                EditorGUI.BeginDisabledGroup(eligibleCount == 0);
                if (GUILayout.Button("平铺贴地"))
                {
                    var p = ResolveParent();
                    if (p == null)
                        return;

                    if (!TryBuildWeightedActivePool(out var pool, out var weights))
                    {
                        EditorUtility.DisplayDialog(
                            "Kenney 地板平铺",
                            "请至少保留一个勾选且权重>0 的 FBX。",
                            "确定");
                        return;
                    }

                    Undo.IncrementCurrentGroup();
                    TileUnderParent(p, pool, weights);
                    MarkActiveSceneDirty();
                }

                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawParentField()
        {
            _parent = (Transform)EditorGUILayout.ObjectField("父节点 Map", _parent, typeof(Transform), true);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("查找场景中的 Map"))
                {
                    var go = GameObject.Find("Map");
                    if (go != null)
                        _parent = go.transform;
                    else
                        EditorUtility.DisplayDialog("Kenney 地板平铺", "场景中未找到名为 Map 的对象。", "确定");
                }

                if (GUILayout.Button("创建空 Map"))
                {
                    var go = new GameObject("Map");
                    Undo.RegisterCreatedObjectUndo(go, "Create Map");
                    Selection.activeTransform = _parent = go.transform;
                    MarkActiveSceneDirty();
                }
            }
        }

        /// <summary>若用户未拖拽父节点：优先场景中 Map，否则对话框并返回 null。</summary>
        private Transform ResolveParent()
        {
            if (_parent != null)
                return _parent;

            var map = GameObject.Find("Map");
            if (map != null)
                return map.transform;

            EditorUtility.DisplayDialog(
                "Kenney 地板平铺",
                "请指定「父节点 Map」，或使用「查找/创建 Map」。",
                "确定");

            return null;
        }

        private void TileUnderParent(Transform parent, List<GameObject> pool, List<float> weights)
        {
            if (parent == null || pool == null || weights == null || pool.Count == 0 || pool.Count != weights.Count)
                return;

            var cyclePool = _variationMode == VariationMode.Cycle
                ? ExpandForWeightedCycle(pool, weights)
                : pool;

            if (cyclePool == null || cyclePool.Count == 0)
                return;

            if (_clearChildrenBeforeTile)
                ClearMapChildren(parent);

            if (_variationMode == VariationMode.Random)
                Random.InitState(_randomSeed);

            var cycle = 0;
            Undo.SetCurrentGroupName(_clearChildrenBeforeTile ? "Kenney 平铺贴地(清空)" : "Kenney 平铺贴地");
            var group = Undo.GetCurrentGroup();

            for (var z = 0; z < _tilesZ; z++)
            {
                for (var x = 0; x < _tilesX; x++)
                {
                    var pick = PickPrefab(pool, weights, cyclePool, ref cycle);
                    if (pick == null)
                        continue;

#if UNITY_2018_3_OR_NEWER
                    var instance = PrefabUtility.InstantiatePrefab(pick, parent) as GameObject;
#else
                    var instance = PrefabUtility.InstantiatePrefab(pick) as GameObject;
                    if (instance != null)
                        Undo.SetTransformParent(instance.transform, parent, "KenneyFloorParent");
#endif
                    if (instance == null)
                        continue;

                    Undo.RegisterCreatedObjectUndo(instance, "Kenney floor tile");

                    instance.transform.localScale = Vector3.one;

                    var localPos = new Vector3(
                        _gridOriginOffset.x + x * _cellSize,
                        _gridOriginOffset.y,
                        _gridOriginOffset.z + z * _cellSize);

                    var yaw = !_randomYawSteps90 ? 0f : Random.Range(0, 4) * 90f;

                    Undo.RecordObject(instance.transform, "KenneyFloorPosition");
                    instance.transform.SetLocalPositionAndRotation(localPos, Quaternion.Euler(0f, yaw, 0f));
                    instance.name = $"Floor_{pick.name}_{x}_{z}";
                }
            }

            Undo.CollapseUndoOperations(group);
        }

        private GameObject PickPrefab(
            List<GameObject> pool,
            List<float> weights,
            List<GameObject> cyclePool,
            ref int cycle)
        {
            if (pool == null || pool.Count == 0)
                return null;

            switch (_variationMode)
            {
                case VariationMode.Random:
                    return WeightedRandomPick(pool, weights);
                case VariationMode.Cycle:
                    if (cyclePool == null || cyclePool.Count == 0)
                        return null;
                    var g = cyclePool[cycle % cyclePool.Count];
                    cycle++;
                    return g;
                default:
                    return pool[0];
            }
        }

        private static GameObject WeightedRandomPick(List<GameObject> pool, List<float> weights)
        {
            if (pool == null || weights == null || pool.Count == 0 || pool.Count != weights.Count)
                return null;

            var sum = 0f;
            for (var i = 0; i < pool.Count; i++)
                sum += Mathf.Max(0f, weights[i]);

            if (sum <= 1e-6f)
                return null;

            var r = Random.value * sum;
            var acc = 0f;
            for (var i = 0; i < pool.Count; i++)
            {
                acc += Mathf.Max(0f, weights[i]);
                if (r <= acc)
                    return pool[i];
            }

            return pool[pool.Count - 1];
        }

        /// <summary>按最小正权重归一化，把每个模型重复若干次形成「比例周期」，供循环模式使用。</summary>
        private static List<GameObject> ExpandForWeightedCycle(List<GameObject> pool, List<float> weights)
        {
            if (pool == null || weights == null || pool.Count != weights.Count || pool.Count == 0)
                return pool;

            float minPos = float.MaxValue;
            for (var i = 0; i < weights.Count; i++)
            {
                var w = Mathf.Max(0f, weights[i]);
                if (w > 1e-6f)
                    minPos = Mathf.Min(minPos, w);
            }

            if (minPos <= 0f || minPos >= float.MaxValue * 0.5f)
                return new List<GameObject>(pool);

            var expanded = new List<GameObject>();
            for (var i = 0; i < pool.Count; i++)
            {
                var w = Mathf.Max(0f, weights[i]);
                if (w <= 1e-6f)
                    continue;
                var reps = Mathf.Max(1, Mathf.RoundToInt(w / minPos));
                for (var r = 0; r < reps; r++)
                    expanded.Add(pool[i]);
            }

            return expanded.Count > 0 ? expanded : new List<GameObject>(pool);
        }

        private void SyncToggleAndWeightLists()
        {
            while (_floorMeshEnabled.Count < _floorMeshes.Count)
                _floorMeshEnabled.Add(true);

            while (_floorMeshEnabled.Count > _floorMeshes.Count)
                _floorMeshEnabled.RemoveAt(_floorMeshEnabled.Count - 1);

            while (_floorMeshWeight.Count < _floorMeshes.Count)
                _floorMeshWeight.Add(1f);

            while (_floorMeshWeight.Count > _floorMeshes.Count)
                _floorMeshWeight.RemoveAt(_floorMeshWeight.Count - 1);
        }

        private int CountEligibleMeshes()
        {
            SyncToggleAndWeightLists();
            var n = 0;
            for (var i = 0; i < _floorMeshes.Count; i++)
            {
                if (_floorMeshes[i] == null)
                    continue;
                if (!_floorMeshEnabled[i])
                    continue;
                if (_floorMeshWeight[i] <= 1e-6f)
                    continue;
                n++;
            }

            return n;
        }

        private void SetAllMeshToggles(bool value)
        {
            SyncToggleAndWeightLists();
            for (var i = 0; i < _floorMeshEnabled.Count; i++)
                _floorMeshEnabled[i] = value;
        }

        private void SetAllWeights(float w)
        {
            SyncToggleAndWeightLists();
            for (var i = 0; i < _floorMeshWeight.Count; i++)
                _floorMeshWeight[i] = Mathf.Max(0f, w);
        }

        private bool TryBuildWeightedActivePool(out List<GameObject> pool, out List<float> weights)
        {
            pool = new List<GameObject>();
            weights = new List<float>();

            SyncToggleAndWeightLists();
            for (var i = 0; i < _floorMeshes.Count; i++)
            {
                if (_floorMeshes[i] == null)
                    continue;
                if (!_floorMeshEnabled[i])
                    continue;
                var w = _floorMeshWeight[i];
                if (w <= 1e-6f)
                    continue;
                pool.Add(_floorMeshes[i]);
                weights.Add(w);
            }

            return pool.Count > 0;
        }

        private void ClearMapChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var c = parent.GetChild(i).gameObject;
                Undo.DestroyObjectImmediate(c);
            }
        }

        private static void MarkActiveSceneDirty()
        {
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private void AutoPickFromFolder(string assetFolderPath)
        {
            assetFolderPath = assetFolderPath?.Replace('\\', '/');

            if (string.IsNullOrEmpty(assetFolderPath))
                assetFolderPath = DefaultKenneyFloorFolder;
            else if (!assetFolderPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Kenney 地板平铺", "路径需位于 Assets 下: " + assetFolderPath, "确定");
                return;
            }

            if (!AssetDatabase.IsValidFolder(assetFolderPath))
            {
                EditorUtility.DisplayDialog("Kenney 地板平铺", "无效的文件夹路径: " + assetFolderPath, "确定");
                return;
            }

            var guids = AssetDatabase.FindAssets("", new[] { assetFolderPath });
            var meshes = new List<GameObject>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetExtension(path).ToLowerInvariant() != ".fbx")
                    continue;

                var name = Path.GetFileNameWithoutExtension(path);
                if (!_includeBalconyFloors &&
                    name.StartsWith("balcony-floor", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!name.StartsWith("floor", System.StringComparison.OrdinalIgnoreCase) &&
                    !name.StartsWith("balcony-floor", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null)
                    meshes.Add(go);
            }

            meshes.Sort((a, b) =>
                string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));

            _floorMeshes = meshes;
            _floorMeshEnabled = Enumerable.Repeat(true, _floorMeshes.Count).ToList();
            _floorMeshWeight = Enumerable.Repeat(1f, _floorMeshes.Count).ToList();
            Debug.Log($"[KenneyFloorTiler] 已载入 {_floorMeshes.Count} 个 floor 网格自 {assetFolderPath}");
            Repaint();
        }
    }
}
#endif
