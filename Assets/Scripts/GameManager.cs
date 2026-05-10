using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 게임 전반의 상태를 관리하는 영구 싱글톤.
// 씬이 바뀌어도 유지되며 무기, 열쇠, 이벤트 진행 여부 등을 저장한다.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("무기 소지 여부")]
    public bool hasGun = false;   // 권총 획득 여부
    public bool hasPipe = false;  // 파이프 획득 여부

    [Header("게임 진행 상태")]
    public int stage = 0;                  // 0=튜토리얼 전, 1=게임 진행 중
    public string currentWeapon = "pipe";  // 현재 장착 무기 문자열 (참고용)

    [Header("열쇠 / 이벤트")]
    public bool hasRightCorridorKey = false; // 오른쪽 복도 열쇠 보유 여부
    public bool gunEventDone = false;        // 총 획득 NPC 이벤트 완료 여부

    // 이벤트 진행 여부를 문자열 키로 추적하는 플래그 집합
    // 예: "introDone", "tutorialDone", "ClockEnd"
    private HashSet<string> flags = new HashSet<string>();

    void Awake()
    {
        // 싱글톤 보장: 이미 존재하면 새 인스턴스 제거
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬 전환 시 HUD 표시 여부 결정.
    // 인트로/튜토리얼은 GameFlowManager.Start()가 직접 HideUI()를 처리하므로
    // stage == 0일 때는 여기서 ShowUI를 호출하지 않는다.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
            UICanvas.Instance?.HideUI();
        else if (stage > 0)
            UICanvas.Instance?.ShowUI();
    }

    // 플래그 등록 (중복 등록 무시)
    public void SetFlag(string flag) => flags.Add(flag);

    // 플래그 보유 여부 반환
    public bool HasFlag(string flag) => flags.Contains(flag);
}
