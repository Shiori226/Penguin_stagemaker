using System;
using System.Text;
using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// CSV エクスポート時に同梱する stage.json のバイト列を生成する。
    /// カスタムステージのときは Maker の Export と完全に同じ JSON フォーマット
    /// (= CustomStageData の JsonUtility 直列化) を出力するので、再インポート可能。
    /// </summary>
    public static class StageInfoExporter
    {
        public const string FileName = "stage.json";

        [Serializable]
        private class DefaultStageInfo
        {
            public string exportedAt;
            public string stageType;
        }

        public static byte[] BuildJsonBytes()
        {
            string json;
            if (StageGenerator.GetStageType() == StageType.Custom)
            {
                var data = StageGenerator.GetLastBuiltCustomStage();
                if (data != null)
                {
                    json = data.ToJson(prettyPrint: true);
                }
                else
                {
                    // フォールバック: 直接読み出せなかった場合は ID だけ書き出す
                    var info = new DefaultStageInfo
                    {
                        exportedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                        stageType = "Custom",
                    };
                    json = JsonUtility.ToJson(info, true);
                }
            }
            else
            {
                var info = new DefaultStageInfo
                {
                    exportedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    stageType = StageGenerator.GetStageType().ToString(),
                };
                json = JsonUtility.ToJson(info, true);
            }

            byte[] preamble = Encoding.UTF8.GetPreamble();
            byte[] body = Encoding.UTF8.GetBytes(json);
            byte[] result = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
            return result;
        }
    }
}
