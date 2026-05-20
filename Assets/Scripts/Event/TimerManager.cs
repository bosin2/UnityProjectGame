using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 제한 시간을 카운트다운하고 UI에 표시하는 컴포넌트.
/// 시간이 0 이 되면 BadEnding 씬으로 전환.
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

        Instance = this;

        // 루트 오브젝트일 때만 DontDestroyOnLoad 적용
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        currentTime = totalTime;
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
            SceneManager.LoadScene("BadEnding");
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
