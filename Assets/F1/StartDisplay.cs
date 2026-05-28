using UnityEngine;
using UnityEngine.UI;

public class StarDisplay : MonoBehaviour
{
    [Header("Star Images")]
    public Image star1;
    public Image star2;
    public Image star3;

    [Header("Colors")]
    public Color fullColor = new Color(1f, 0.85f, 0f);  // gold
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f); // grey

    public void SetStars(float starFloat)
    {
        SetStar(star1, starFloat - 0f);
        SetStar(star2, starFloat - 1f);
        SetStar(star3, starFloat - 2f);
    }

    void SetStar(Image img, float value)
    {
        if (img == null) return;

        if (value >= 1f)
        {
            // Full star
            img.type = Image.Type.Simple;
            img.color = fullColor;
            img.fillAmount = 1f;
        }
        else if (value >= 0.5f)
        {
            // Half star — filled halfway
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 0.5f;
            img.color = fullColor;
        }
        else
        {
            // Empty star
            img.type = Image.Type.Simple;
            img.color = emptyColor;
            img.fillAmount = 1f;
        }
    }
}