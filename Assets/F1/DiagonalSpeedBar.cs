using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class DiagonalSpeedBar : MaskableGraphic
{
    [Range(0f, 1f)]
    public float fillAmount = 0f;

    [Header("Colors")]
    public Color slowColor = new Color(0.1f, 0.9f, 0.1f);
    public Color midColor = new Color(1.0f, 0.8f, 0.0f);
    public Color fastColor = new Color(0.9f, 0.1f, 0.1f);

    [Header("Shape")]
    public float skewOffset = 20f;

    public void SetFill(float value)
    {
        fillAmount = Mathf.Clamp01(value);
        SetVerticesDirty();
    }

    Color GetSpeedColor()
    {
        if (fillAmount < 0.5f)
            return Color.Lerp(slowColor, midColor, fillAmount * 2f);
        return Color.Lerp(midColor, fastColor, (fillAmount - 0.5f) * 2f);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect r = rectTransform.rect;
        float maxW = r.width;
        float w = maxW * fillAmount;
        float h = r.height;
        float x = r.xMin;
        float y = r.yMin;
        float skew = skewOffset;

        Color c = GetSpeedColor();

        UIVertex v0 = UIVertex.simpleVert;
        v0.color = c;
        v0.position = new Vector3(x, y);

        UIVertex v1 = UIVertex.simpleVert;
        v1.color = c;
        v1.position = new Vector3(x + w, y);

        UIVertex v2 = UIVertex.simpleVert;
        v2.color = c;
        v2.position = new Vector3(x + w + skew, y + h);

        UIVertex v3 = UIVertex.simpleVert;
        v3.color = c;
        v3.position = new Vector3(x + skew, y + h);

        vh.AddVert(v0);
        vh.AddVert(v1);
        vh.AddVert(v2);
        vh.AddVert(v3);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(0, 2, 3);
    }
}