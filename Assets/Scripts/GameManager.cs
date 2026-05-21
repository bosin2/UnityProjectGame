using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전반의 상태를 관리하는 영구 싱글톤.
/// 씬이 바뀌어도 유지되며 무기 소지, 열쇠, 이벤트 플래그, NPC phase를 저장한다.
///
/// [다른 스크립트에서 접근법]
/// GameManager.Instance.SetFlag("flagName") 처럼 직접 호출.
/// GameManager는 전역 상태 전용이므로 싱글톤을 유지한다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("무기 소지 여부")]
    public bool hasGun  = false;
    public bool hasPipe = false;

    [Header("게임 진행 상태")]
    public int    stage         = 0;          // 0=튜토리얼 전, 1=게임 진행 중
    public string currentWeapon = "pipe";     // 참고용 문자열

    [Header("열쇠 / 이벤트")]
    public bool hasRightCorridorKey = false;
    public bool gunEventDone        = false;

    // 이벤트 진행 플래그 집합 ("introDone", "tutorialDone", "ClockEnd" 등)
    private HashSet<string>        flags        = new HashSet<string>();
    // Interactable phase 인덱스 씬 간 유지용
    private Dictionary<string,int> phaseIndices = new Dictionary<string,int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
            UICanvas.Instance?.HideUI();
        else if (stage > 0)
            UICanvas.Instance?.ShowUI();
    }

    // ── 플래그 관리 ───────────────────────────────────────────────────

    public void SetFlag(string flag)        => flags.Add(flag);
    public bool HasFlag(string flag)        => flags.Contains(flag);

    // ── Phase 인덱스 관리 ─────────────────────────────────────────────

    public void SetPhaseIndex(string id, int index) => phaseIndices[id] = index;
    public int  GetPhaseIndex(string id) =>
        phaseIndices.TryGetValue(id, out int v) ? v : 0;

    // ── 게임 리셋 ─────────────────────────────────────────────────────

    public void ResetGame()
    {
        hasGun              = false;
        hasPipe             = false;
        hasRightCorridorKey = false;
        gunEventDone        = false;
        stage               = 0;
        currentWeapon       = "pipe";
        flags.Clear();
        phaseIndices.Clear();
    }
}
