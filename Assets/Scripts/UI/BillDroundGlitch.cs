using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// 게임 내 전광판이 주기적으로 글리치 효과를 내는 연출 컴포넌트
public class BillDroundGlitch : MonoBehaviour
{
    public Image billboardImage;

    // 현재 사용 중인 텍스처 참조 (이전 텍스처 해제용)
    private Texture2D activeTexture;

    void Start()
    {
        billboardImage.sprite = CreateSprite(false);
        StartCoroutine(GlitchLoop());
    }

    // 일정 간격으로 노이즈 스프라이트를 여러 번 교체하는 글리치 루프
    IEnumerator GlitchLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(2f, 3f));

            int glitchCount = Random.Range(4, 10);
            for (int i = 0; i < glitchCount; i++)
            {
                billboardImage.sprite = CreateSprite(true);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.07f));
            }

            billboardImage.sprite = CreateSprite(false);
        }
    }

    // 스프라이트 생성 시 이전 텍스처를 해제해 메모리 누수 방지.
    // isNoise=true면 랜덤 노이즈, false면 검정 텍스처 생성
    Sprite CreateSprite(bool isNoise)
    {
        if (activeTexture != null)
            Destroy(activeTexture);

        int w = 64, h = 16;
        activeTexture = new Texture2D(w, h);
        activeTexture.filterMode = FilterMode.Point;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Color col;
                if (isNoise)
                    col = Random.value > 0.3f
                        ? new Color(Random.value, Random.value, Random.value)
                        : Color.black;
                else
                    col = Color.black;

                activeTexture.SetPixel(x, y, col);
            }
        }

        activeTexture.Apply();
        return Sprite.Create(activeTexture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    // 오브젝트 제거 시 마지막 텍스처 메모리 해제
    void OnDestroy()
    {
        if (activeTexture != null)
            Destroy(activeTexture);
    }
}
