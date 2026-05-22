using System;
using System.Collections.Generic;
using UnityEngine;

namespace StageMaker
{
    public enum StagePartCategory
    {
        Platform = 0,
        Item = 1,
        Enemy = 2,
        Gimmick = 3,
        Special = 4,
    }

    [Serializable]
    public class StagePartDefinition
    {
        public string id;
        public string displayName;
        public StagePartCategory category;
        public Color paletteColor = Color.white;
        public GameObject prefab;
        public bool unique;
        public Vector3 spawnOffset = Vector3.zero;
        public bool requiresEvenRow;

        /// <summary>
        /// true のとき、エディタで配置時に追加で「方向ハンドル」が出現し、
        /// CustomStagePartPlacement.directionTarget を設定できる。
        /// </summary>
        public bool isDirectional;

        /// <summary>
        /// 方向系パーツの runtime 種別 (MovingIce / Blizzard / Seal)
        /// </summary>
        public string directionalKind;
    }

    [CreateAssetMenu(menuName = "SlidingPenguin/Stage Part Catalog", fileName = "StagePartCatalog")]
    public class StagePartCatalog : ScriptableObject
    {
        [SerializeField] private List<StagePartDefinition> parts = new();

        public IReadOnlyList<StagePartDefinition> Parts => parts;

        public StagePartDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) { return null; }
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != null && parts[i].id == id) { return parts[i]; }
            }
            return null;
        }

        private static StagePartCatalog cached;

        public static StagePartCatalog Load()
        {
            if (cached != null) { return cached; }
            cached = Resources.Load<StagePartCatalog>("StageMaker/StagePartCatalog");
            if (cached == null)
            {
                Debug.LogError("[StagePartCatalog] StageMaker/StagePartCatalog asset not found in Resources.");
            }
            return cached;
        }
    }
}
