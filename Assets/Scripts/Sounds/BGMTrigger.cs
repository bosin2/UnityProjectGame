/*
 * BGMTrigger.cs
 * 역할: 플레이어가 특정 구역에 들어왔을 때 BGM을 바꾸는 트리거 스크립트입니다.
 * 연결: AudioManager의 BGM 라이브러리 이름을 문자열로 호출합니다.
 * 주의: 호출 이름이 AudioManager의 bgmClips 이름과 다르면 경고만 출력되고 음악은 바뀌지 않습니다.
 */using UnityEngine;

// 플레이어가 트리거 영역 진입 시 BGM 교체.
// 보스방 입구, 특정 이벤트 구역 등에 배치.
[RequireComponent(typeof(Collider2D))]
public class BGMTrigger : MonoBehaviour
{
    [Header("교체할 BGM 이름")]
    [SerializeField] private string bgmName;

    [Range(0f, 1f)]
    [SerializeField] private float volumeScale = 1f;

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

        AudioManager.Instance?.PlayBGM(bgmName, volumeScale: volumeScale);
        triggered = true;

        Debug.Log($"[BGMTrigger] '{bgmName}' BGM으로 전환");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        RevertBGM();
    }

    void OnDisable()
    {
        // 씬 전환 시 OnTriggerExit2D가 호출되지 않으므로 OnDisable에서 복귀
        if (triggered) RevertBGM();
    }

    void RevertBGM()
    {
        if (!revertOnExit) return;
        if (string.IsNullOrEmpty(revertBgmName)) return;

        AudioManager.Instance?.PlayBGM(revertBgmName);
        triggered = false;
        Debug.Log($"[BGMTrigger] '{revertBgmName}'로 복귀");
    }
}


