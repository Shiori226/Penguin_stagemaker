using UnityEngine;
using UnityEngine.EventSystems;

namespace StageMaker
{
    /// <summary>
    /// パレット項目をドラッグして 3D シーンにパーツを配置するためのハンドラ。
    /// IBeginDragHandler / IDragHandler / IEndDragHandler を使用してドラッグ操作を受け取る。
    /// </summary>
    public class PaletteDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private StageMakerEditorView editor;
        private string partId;
        private RectTransform hoverTarget;
        private GameObject ghost;
        private DraggablePart ghostDraggable;
        private bool hovering;

        public void Initialize(StageMakerEditorView e, string id, RectTransform hoverTargetOverride = null)
        {
            editor = e;
            partId = id;
            hoverTarget = hoverTargetOverride;
        }

        private void Update()
        {
            if (hoverTarget == null || partId == StageMakerEditorView.EraserId) { return; }
            float targetScale = hovering ? 1.12f : 1f;
            hoverTarget.localScale = Vector3.Lerp(hoverTarget.localScale, Vector3.one * targetScale, Time.unscaledDeltaTime * 12f);
            PalettePreviewRenderer.AnimatePreview(partId, hovering, Time.unscaledDeltaTime);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (editor == null) return;
            // クリックでパーツ選択 (消しゴム選択のため)
            editor.SelectPaletteItem(partId);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (editor == null) return;
            if (partId == StageMakerEditorView.EraserId) return;

            editor.SelectPaletteItem(partId);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (editor.TryRaycastGround(eventData.position, out Vector3 hitPoint))
            {
                if (ghost == null)
                {
                    ghost = editor.SpawnGhostFromPalette(partId, eventData);
                    if (ghost != null)
                    {
                        ghostDraggable = ghost.GetComponent<DraggablePart>();
                    }
                }
                if (ghost == null) { return; }

                Vector3 newPos = hitPoint + (ghostDraggable != null && ghostDraggable.definition != null
                    ? ghostDraggable.definition.spawnOffset
                    : Vector3.zero);
                Vector3 delta = newPos - ghost.transform.position;
                ghost.transform.position = newPos;

                if (ghostDraggable != null && ghostDraggable.placement != null)
                {
                    ghostDraggable.placement.worldPosition = hitPoint;
                    // 方向ハンドル付きの場合は同じ delta だけ動かしてリンクを更新
                    if (ghostDraggable.partner != null)
                    {
                        // Blizzard ハンドルは本体に対して固定半径
                        if (ghostDraggable.partner.definition != null && ghostDraggable.partner.definition.directionalKind == "Blizzard")
                        {
                            Vector3 newHandlePos = StageMakerEditorView.ConstrainHandlePosition(ghostDraggable.partner, ghostDraggable.partner.transform.position + delta);
                            ghostDraggable.partner.transform.position = newHandlePos;
                            ghostDraggable.placement.directionTarget = newHandlePos;
                        }
                        else
                        {
                            ghostDraggable.partner.transform.position += delta;
                            ghostDraggable.placement.directionTarget += delta;
                        }
                        StageMakerEditorView.UpdateLinkLine(ghostDraggable);
                    }
                    // Blizzard はリアルタイムに風向きを更新
                    if (ghostDraggable.definition != null
                        && ghostDraggable.definition.directionalKind == "Blizzard")
                    {
                        StageMakerEditorView.ApplyBlizzardWindLive(ghostDraggable);
                    }
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (ghost == null) return;
            // ドラッグ終了時にゴーストが地面より上にある場合のみ確定
            bool overScene = editor.TryRaycastGround(eventData.position, out _);
            editor.FinalizeGhost(ghost, ghostDraggable, accepted: overScene);
            ghost = null;
            ghostDraggable = null;
        }
    }
}
