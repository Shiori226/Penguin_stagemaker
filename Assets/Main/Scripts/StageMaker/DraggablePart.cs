using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// 編集中ステージに置かれた 3D パーツに付くマーカ MonoBehaviour。
    /// 入力ハンドリングはエディタ本体 (StageMakerEditorView) が一括管理するため、
    /// このコンポーネントはデータ参照だけを保持する。
    ///
    /// 方向性のあるパーツ (Moving Ice / Blizzard / Seal) には isHandle=true の対の
    /// DraggablePart (方向ハンドル) が紐づく。owner は本体側を指す。
    /// </summary>
    public class DraggablePart : MonoBehaviour
    {
        public CustomStagePartPlacement placement;
        public StagePartDefinition definition;

        public bool isHandle;            // true: 方向ハンドル / false: 本体パーツ
        public DraggablePart partner;    // 本体 ↔ ハンドルの相互参照
        public LineRenderer linkLine;    // 本体側に持たせる接続線

        public void Initialize(CustomStagePartPlacement p, StagePartDefinition def)
        {
            placement = p;
            definition = def;
        }
    }
}
