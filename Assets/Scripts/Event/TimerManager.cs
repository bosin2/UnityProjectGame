using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

// 게임 제한 시간을 카운트다운하고 UI에 표시하는 싱글톤. 0이 되면 배드엔딩 씬으로 전환
public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance;

    [Header("설정")]
    public float totalTime = 900f; // 15분 (초 단위)
    private float currentTime;
    private bool isRunning = true;

    [Header("UI")]
    public TextMeshProUGUI timerText;

    void Awake()
    {
        if (timerText == null)
        {
            Debug.LogWarning("[TimerManager] timerText is missing. Disabling this component.", this);
            enabled = false;
            return;
        }

        if (Instance == null)
        {
            Instance = this;

            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
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
            isRunning = false;
            GameOver();
        }

        UpdateUI();
    }

    // 남은 시간을 MM:SS 형식으로 표시. 3분 이하면 빨간색으로 변경
    void UpdateUI()
    {
        if (timerText == null || !timerText.gameObject.activeInHierarchy) return;
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerText.color = currentTime <= 180f ? Color.red : Color.white;
    }

    // 타이머 일시 정지
    public void PauseTimer() => isRunning = false;

    // 타이머 재개
    public void ResumeTimer() => isRunning = true;

    // 시간 초과 시 배드엔딩 씬으로 전환
    void GameOver()
    {
        SceneManager.LoadScene("BadEnding");
    }
}
