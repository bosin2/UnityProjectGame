using UnityEngine;

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
        manager?.StartCutscene();
    }
}