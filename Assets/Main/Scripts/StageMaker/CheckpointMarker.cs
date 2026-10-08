using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// Visual marker that can be placed and saved in the Stage Maker.
    /// Gameplay checkpoints are still owned by ice platforms.
    /// </summary>
    public class CheckpointMarker : MonoBehaviour
    {
        private const string VisualRootName = "CheckpointMarkerVisual";

        private void Awake()
        {
            BuildVisualIfNeeded();
        }

        private void BuildVisualIfNeeded()
        {
            if (transform.Find(VisualRootName) != null)
            {
                return;
            }

            var visualRoot = new GameObject(VisualRootName);
            visualRoot.transform.SetParent(transform, false);

            CreatePrimitive(PrimitiveType.Cylinder, visualRoot.transform,
                "Pole", new Vector3(0f, 0.65f, 0f), new Vector3(0.06f, 0.65f, 0.06f),
                new Color(0.25f, 0.85f, 1f, 1f));
            CreatePrimitive(PrimitiveType.Sphere, visualRoot.transform,
                "Beacon", new Vector3(0f, 1.35f, 0f), Vector3.one * 0.22f,
                new Color(0.75f, 0.98f, 1f, 1f));
            CreatePrimitive(PrimitiveType.Cube, visualRoot.transform,
                "Flag", new Vector3(0.27f, 1.12f, 0f), new Vector3(0.48f, 0.22f, 0.04f),
                new Color(0.1f, 0.65f, 0.95f, 1f));
        }

        private static void CreatePrimitive(
            PrimitiveType primitiveType,
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(collider);
                }
                else
                {
                    Object.DestroyImmediate(collider);
                }
            }

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader != null)
            {
                renderer.material = new Material(shader);
                renderer.material.color = color;
            }
            else
            {
                renderer.material.color = color;
            }
        }
    }
}
