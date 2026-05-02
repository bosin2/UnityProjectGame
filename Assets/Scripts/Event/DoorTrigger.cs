using UnityEngine;
using UnityEngine.SceneManagement;

// 플레이어가 문 트리거에 진입하면 지정 씬으로 전환하고 스폰 위치를 저장하는 컴포넌트
public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private string targetSceneName;   // 이동할 씬 이름
    [SerializeField] private Vector2 spawnPosition;    // 도착 씬에서의 스폰 위치

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // PlayerMovement.OnSceneLoaded에서 읽어 플레이어 위치에 적용
        PlayerPrefs.SetFloat("SpawnX", spawnPosition.x);
        PlayerPrefs.SetFloat("SpawnY", spawnPosition.y);

        SceneManager.LoadScene(targetSceneName);
    }
}
