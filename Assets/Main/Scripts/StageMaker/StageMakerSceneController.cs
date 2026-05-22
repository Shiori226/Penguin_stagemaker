using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StageMaker
{
    /// <summary>
    /// Stage Maker シーンのエントリポイント。
    /// 一覧ビューと編集ビューの生成・切替を担当する。
    /// </summary>
    public class StageMakerSceneController : MonoBehaviour
    {
        private GameObject rootCanvas;
        private GameObject listView;
        private StageMakerListView listController;
        private GameObject editorView;
        private StageMakerEditorView editorController;

        private List<CustomStageData> customStages = new();

        private void Awake()
        {
            EnsureEventSystem();
            BuildRootUI();
            CreateSeaBackground();

            // 試遊から戻った場合は編集していたステージの編集画面を直接開く
            var returnId = StageGenerator.ReturnToEditStageId;
            if (!string.IsNullOrEmpty(returnId))
            {
                StageGenerator.SetReturnToEditStageId(string.Empty);
                var data = CustomStageRepository.Load(returnId);
                if (data != null)
                {
                    EnterEditor(data);
                    return;
                }
            }

            ShowList();
        }

        private void CreateSeaBackground()
        {
            var mat = Resources.Load<Material>("StageMaker/Sea");
            if (mat == null) return;

            var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.name = "StageMakerSea";
            sea.transform.position = new Vector3(0f, -0.5f, 30f);
            sea.transform.localScale = new Vector3(20f, 1f, 20f); // 200x200m でカメラ全域をカバー
            sea.GetComponent<MeshRenderer>().material = mat;
            // 判定不要なのでコライダーを除去
            var col = sea.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private void EnsureEventSystem()
        {
            var existing = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (existing != null) { return; }
            var go = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
            go.transform.SetParent(transform, false);
        }

        private void BuildRootUI()
        {
            rootCanvas = StageMakerUIFactory.CreateCanvas("MakerCanvas", 0);
            rootCanvas.transform.SetParent(transform, false);
            // ※ ScreenSpace Overlay の Canvas に全面背景を置くと 3D ビューが完全に隠れるので、
            //   ここではフルスクリーン背景を作らず、各ビュー側で必要に応じて背景を持つ。
        }

        public void ShowList()
        {
            if (editorView != null) { editorView.SetActive(false); }

            if (listView == null)
            {
                listView = new GameObject("ListView", typeof(RectTransform));
                var rt = listView.GetComponent<RectTransform>();
                rt.SetParent(rootCanvas.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                listController = listView.AddComponent<StageMakerListView>();
                listController.Initialize(this);
            }
            listView.SetActive(true);
            ReloadStages();
            listController.Refresh(customStages);
        }

        public void ReloadStages()
        {
            customStages = CustomStageRepository.LoadAll();
        }

        public void EnterEditor(CustomStageData data)
        {
            if (listView != null) { listView.SetActive(false); }

            if (editorView == null)
            {
                editorView = new GameObject("EditorView", typeof(RectTransform));
                var rt = editorView.GetComponent<RectTransform>();
                rt.SetParent(rootCanvas.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                editorController = editorView.AddComponent<StageMakerEditorView>();
                editorController.Initialize(this);
            }
            editorView.SetActive(true);
            editorController.LoadStage(data);
        }

        public void PlayDefaultStage(StageType stageType)
        {
            StageGenerator.SetStageType(stageType);
            StageGenerator.SetSelectedCustomStageId(string.Empty);
            LoadInGame();
        }

        public void PlayCustomStage(string stageId)
        {
            StageGenerator.SetStageType(StageType.Custom);
            StageGenerator.SetSelectedCustomStageId(stageId);
            StageGenerator.SetTestPlay(true);
            StageGenerator.SetReturnToEditStageId(stageId);
            LoadInGame();
        }

        public void BackToTitle()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.bgm.Stop();
                AudioManager.Instance.bgm.SetPitch(1.0f);
            }
            SceneManager.LoadScene("Title");
        }

        private void LoadInGame()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.bgm.Stop();
                AudioManager.Instance.bgm.SetPitch(1.0f);
            }
            if (DataLogger.Instance != null) { DataLogger.Instance.AbortTrial(); }
            SceneManager.LoadScene("InGame");
        }
    }
}
