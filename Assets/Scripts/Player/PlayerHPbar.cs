using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// 플레이어 HP를 슬라이더와 색상으로 표시하는 순수 UI 컴포넌트.
// HP 데이터는 PlayerMovement가 소유하며, 이 클래스는 표시만 담당한다.
public class PlayerHPbar : MonoBehaviour
{
    public static PlayerHPbar Instance;

    [Header("UI 연결")]
    public Slider hpSlider;
    public Image fillImage;
    public GameObject damageFlash; // 피격 시 화면 가장자리 빨간 플래시 오브젝트

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 씬 로드 시 현재 PlayerMovement의 HP 값을 읽어 UI 초기화
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player != null)
            Refresh(player.CurrentHp, player.maxHp, false);
    }

    // HP 슬라이더와 색상 갱신. showFlash=true이면 피격 플래시 효과도 재생
    public void Refresh(int current, int max, bool showFlash = true)
    {
        float ratio = (float)current / max;

        hpSlider.value = ratio;
        fillImage.color = ratio > 0.5f ? Color.green
                        : ratio > 0.25f ? Color.yellow
                        : Color.red;

        if (showFlash)
            StartCoroutine(FlashEffect());
    }

    // 피격 시 화면 가장자리에 0.2초간 빨간 플래시 표시
    IEnumerator FlashEffect()
    {
        if (damageFlash != null)
        {
            damageFlash.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            damageFlash.SetActive(false);
        }
    }
}
