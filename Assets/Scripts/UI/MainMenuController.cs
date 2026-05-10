using UnityEngine;
using UnityEngine.SceneManagement;

// 메인 메뉴 버튼 이벤트 핸들러
public class MainMenuController : MonoBehaviour
{
    // 게임 시작 버튼: GameLap 씬으로 전환 (인트로/튜토리얼은 GameFlowManager 가 처리)
    public void OnStartGame()
    {
        SceneManager.LoadScene("GameLap");
    }

    // 설정 버튼 (미구현)
    public void OnSettings()
    {
    }

    // 종료 버튼: 애플리케이션 종료
    public void OnQuit()
    {
        Application.Quit();
    }
}
