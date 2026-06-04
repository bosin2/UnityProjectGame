/*
 * TimerManager.cs
 * 역할: 제한 시간 UI와 타이머 종료 시 게임오버 흐름을 관리하는 전역 타이머입니다.
 * 연결: CorridorIntroManager/RooftopManager가 컷씬 중 타이머를 멈추고, PlayerHealth가 타이머 만료 게임오버와 연결됩니다.
 * 주의: Time.timeScale이 0인 대화/인벤토리 상황과 별도로 의도한 일시정지 상태를 유지해야 합니다.
 */using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 제한 시간을 카운트다운하고 UI에 표시하는 컴포넌트.
/// 시간이 0 이 되면 PlayerHealth.TriggerGameOver()를 호출한다.
/// UICanvas의 일부로 씬 전환 후에도 유지된다.
/// </summary>
public class TimerManager : MonoBehaviour
{
    // 소프트 참조
    public static TimerManager Instance { get; private set; }

    [Header("제한 시간 (초)")]
    public float totalTime = 900f; // 15분

    [Header("UI")]
    public TextMeshProUGUI timerText;

    private float currentTime;
    private bool  isRunning = true;

    void Awake()
    {
        if (timerText == null)
        {
            Debug.LogWarning("[TimerManager] timerText가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        // 중복 방지 (UICanvas 와 동일한 패턴)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 루트 오브젝트일 때만 DontDestroyOnLoad 적용
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        currentTime = totalTime;
    }

    /// <summary>게임 재시작 시 타이머를 처음 값으로 되돌린다. GameManager.ResetGame()에서 호출.</summary>
    public void ResetTimer()
    {
        currentTime = totalTime;
        isRunning   = true;
        UpdateUI();
    }

    void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isRunning   = false;
            UpdateUI();
            PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
            if (player != null)
                player.TriggerGameOver();
            else
                SceneManager.LoadScene("MainMenu");
            return;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (timerText == null || !timerText.gameObject.activeInHierarchy) return;

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        timerText.text  = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerText.color = currentTime <= 180f ? Color.red : Color.white;
    }

    public void PauseTimer()  => isRunning = false;
    public void ResumeTimer() => isRunning = true;
}



