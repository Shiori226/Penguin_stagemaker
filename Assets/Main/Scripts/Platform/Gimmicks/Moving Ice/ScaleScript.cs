using UnityEngine;

public class ScaleScript : MonoBehaviour
{
    private Transform targetParent = null;

    private void LateUpdate()
    {
        var p = (targetParent != null ? targetParent : transform.parent);
        if (p == null) return;

        var s = p.lossyScale;
        // 0Š„‚è–h~
        float ix = Mathf.Approximately(s.x, 0f) ? 0f : 1f / s.x;
        float iy = Mathf.Approximately(s.y, 0f) ? 0f : 1f / s.y;
        float iz = Mathf.Approximately(s.z, 0f) ? 0f : 1f / s.z;

        // eƒXƒP[ƒ‹‚Ì‹t”‚ğŠ|‚¯‚é‚±‚Æ‚ÅAParent*Isolator ‚ªí‚É 1,1,1 ‚É‚È‚é
        transform.localScale = new Vector3(ix, iy, iz);
    }
}
