using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 HP를 슬라이더 UI로 표시하는 컴포넌트.
/// PlayerHealth.OnHPChanged 이벤트를 구독해 반응형으로 갱신한다.
/// UICanvas의 일부로 씬 전환 후에도 유지된다.
/// </summary>
public class PlayerHPbar : MonoBehaviour
{
    [Header("UI 연결")]
    public Slider hpSlider;
    public Image  fillImage;
    public GameObject damageFlash; // 피격 시 깜빡이는 오브젝트

    private PlayerHealth playerHealth;
    private Coroutine flashRoutine;

    void Awake()
    {
        if (hpSlider == null || fillImage == null)
        {
            Debug.LogWarning("[PlayerHPbar] HP UI 참조가 없습니다. 컴포넌트를 비활성화합니다.", this);
            enabled = false;
            return;
        }

        // 루트 오브젝트일 때만 DontDestroyOnLoad 적용
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        TryBindToCurrentPlayer();
    }

    void Update()
    {
        if (playerHealth == null || !playerHealth.gameObject.activeInHierarchy)
            TryBindToCurrentPlayer();
    }

    void OnDisable()
    {
        UnsubscribeFromPlayer();
    }

    void TryBindToCurrentPlayer()
    {
        PlayerHealth current = FindCurrentPlayerHealth();
        if (current == null || current == playerHealth) return;

        UnsubscribeFromPlayer();
        playerHealth = current;
        playerHealth.OnHPChanged += OnHPChanged;
        Refresh(playerHealth.CurrentHp, playerHealth.maxHp, false);
    }

    PlayerHealth FindCurrentPlayerHealth()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && player.activeInHierarchy)
            return player.GetComponent<PlayerHealth>();

        return FindFirstObjectByType<PlayerHealth>();
    }

    void UnsubscribeFromPlayer()
    {
        if (playerHealth != null)
            playerHealth.OnHPChanged -= OnHPChanged;
        playerHealth = null;
    }

    /// <summary>HP 비율에 따라 슬라이더와 색상을 갱신. 선택적으로 피격 플래시 재생</summary>
    public void Refresh(int current, int max, bool showFlash = true)
    {
        if (hpSlider == null || fillImage == null || max <= 0) return;

        float ratio = Mathf.Clamp01((float)current / max);
        hpSlider.value = ratio;
        fillImage.color = Color.red;

        if (showFlash && gameObject.activeInHierarchy)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashEffect());
        }
    }

    System.Collections.IEnumerator FlashEffect()
    {
        if (damageFlash != null)
        {
            damageFlash.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            damageFlash.SetActive(false);
        }
        flashRoutine = null;
    }

    private void OnHPChanged(int current, int max) => Refresh(current, max, true);

    void OnDestroy()
    {
        UnsubscribeFromPlayer();
    }
}