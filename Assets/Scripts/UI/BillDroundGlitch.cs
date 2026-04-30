using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BillDroundGlitch : MonoBehaviour
{
    public Image billboardImage;

    void Start()
    {
        billboardImage.sprite = MakeBlackSprite();
        StartCoroutine(GlitchLoop());
    }

    IEnumerator GlitchLoop()
    {
        while (true)
        {
            // 평상시 대기
            yield return new WaitForSeconds(Random.Range(2f, 3f));

            // 글리치 발동
            int glitchCount = Random.Range(4, 10);
            for (int i = 0; i < glitchCount; i++)
            {
                billboardImage.sprite = MakeNoiseSprite();
                yield return new WaitForSeconds(Random.Range(0.05f, 0.07f));
            }

            billboardImage.sprite = MakeBlackSprite();
        }
    }
    Sprite MakeBlackSprite()
    {
        int w = 64, h = 16;
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, Color.black);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    Sprite MakeNoiseSprite()
    {
        int w = 64, h = 16;
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Color col = Random.value > 0.3f
                    ? new Color(Random.value, Random.value, Random.value)
                    : Color.black;
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }
}