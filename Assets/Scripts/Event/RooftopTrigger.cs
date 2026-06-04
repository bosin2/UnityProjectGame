/*
 * RooftopTrigger.cs
 * 역할: 플레이어가 옥상 컷씬 시작 구역에 들어오면 RooftopManager에게 컷씬 시작을 요청합니다.
 * 연결: Rooftop 씬의 RooftopManager 참조를 Inspector에서 받아 StartCutscene을 호출합니다.
 * 주의: 트리거가 여러 번 실행되어도 RooftopManager 내부에서 중복 시작을 막아야 합니다.
 */using UnityEngine;

/// <summary>
/// 옥상 절반 지점에 깔아두는 트리거.
/// 플레이어가 들어오면 RooftopCutsceneManager에 시작 신호를 보낸다.
/// 한 번만 발동되고, 발동 후에는 무효화된다.
/// </summary>
public class RooftopTrigger : MonoBehaviour
{
    [Tooltip("씬에 배치된 RooftopCutsceneManager")]
    public RooftopManager manager;

    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        AudioManager.Instance?.PlayBGM("rooftop");
        manager?.StartCutscene();
    }
}
