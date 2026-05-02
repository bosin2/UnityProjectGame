using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance;

    [Header("설정")]
    public float totalTime = 900f; // 15분
    private float currentTime;
    private bool isRunning = true;

    [Header("UI")]
    public TextMeshProUGUI timerText;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
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

    void UpdateUI()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        // 3분 이하면 빨간색
        timerText.color = currentTime <= 180f ? Color.red : Color.white;
    }

    public void PauseTimer() => isRunning = false;
    public void ResumeTimer() => isRunning = true;

    void GameOver()
    {
        // 배드엔딩 씬으로 전환
        SceneManager.LoadScene("BadEnding");
    }
}