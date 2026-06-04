/*
 * MainMenuController.cs
 * 역할: 메인 메뉴 버튼 입력을 받아 새 게임 시작, 종료 등 메뉴 흐름을 처리합니다.
 * 연결: GameManager.ResetGame, SceneManager.LoadScene, AudioManager와 맞물려 게임 시작 상태를 초기화합니다.
 * 주의: 새 게임 시작 시 이전 플레이의 플래그/몬스터 상태/타이머가 남지 않도록 GameManager 초기화를 고려해야 합니다.
 */using UnityEngine;
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

