using System;
using System.Collections.Generic;
using UnityEngine;

namespace StageMaker
{
    [Serializable]
    public class CustomStagePartPlacement
    {
        public string partId;
        public Vector3 worldPosition;
        public float rotationY;

        /// <summary>
        /// 方向性のあるパーツ (Moving Ice / Blizzard / Seal) のターゲット位置 (絶対ワールド座標)。
        /// それ以外のパーツでは無視される。
        /// </summary>
        public Vector3 directionTarget;
    }

    [Serializable]
    public class CustomStageData
    {
        public const int CurrentSchemaVersion = 3;

        public int schemaVersion = CurrentSchemaVersion;
        public string id = "";
        public string displayName = "Untitled";
        public string createdAt = "";
        public string updatedAt = "";
        public List<CustomStagePartPlacement> parts = new();

        public static CustomStageData CreateNew(string name)
        {
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            // Start / Goal はビルダーが固定位置に置くので、parts には含めない。
            return new CustomStageData
            {
                id = Guid.NewGuid().ToString("N"),
                displayName = string.IsNullOrEmpty(name) ? "Untitled" : name,
                createdAt = now,
                updatedAt = now,
            };
        }

        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        public static CustomStageData FromJson(string json)
        {
            return JsonUtility.FromJson<CustomStageData>(json);
        }
    }
}
