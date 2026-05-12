using UnityEngine;

// 플레이어가 트리거 영역 진입 시 BGM 교체.
// 보스방 입구, 특정 이벤트 구역 등에 배치.
[RequireComponent(typeof(Collider2D))]
public class BGMTrigger : MonoBehaviour
{
    [Header("교체할 BGM 이름")]
    [SerializeField] private string bgmName;

    [Header("옵션")]
    [Tooltip("한 번만 작동할지")]
    [SerializeField] private bool triggerOnce = true;

    [Tooltip("나갈 때 원래 BGM으로 복귀할지")]
    [SerializeField] private bool revertOnExit = false;

    [Tooltip("나갈 때 복귀시킬 BGM 이름")]
    [SerializeField] private string revertBgmName;

    private bool triggered = false;

    void Awake()
    {
        // 콜라이더를 트리거로 강제 설정
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Player 태그가 아니면 무시
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && triggered) return;

        AudioManager.Instance?.PlayBGM(bgmName);
        triggered = true;

        Debug.Log($"[BGMTrigger] '{bgmName}' BGM으로 전환");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!revertOnExit) return;
        if (string.IsNullOrEmpty(revertBgmName)) return;

        AudioManager.Instance?.PlayBGM(revertBgmName);
        Debug.Log($"[BGMTrigger] '{revertBgmName}'로 복귀");
    }
}