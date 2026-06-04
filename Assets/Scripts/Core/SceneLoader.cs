/*
 * SceneLoader.cs
 * 역할: 씬 전환 직전에 플레이어 스폰 위치와 바라볼 방향을 임시 저장하는 정적 유틸리티입니다.
 * 연결: DoorTrigger가 LoadScene을 호출하고, PlayerMovement.OnSceneLoaded가 TryConsumePendingSpawn으로 값을 꺼냅니다.
 * 주의: SpawnData는 한 번 소비되면 지워지므로, 같은 씬 로드에서 여러 시스템이 동시에 소비하지 않게 해야 합니다.
 */using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 유틸리티 (PlayerPrefs 대신 메모리로 스폰 데이터를 전달).
/// DoorTrigger → SceneLoader.LoadScene() → PlayerMovement.OnSceneLoaded() 순서로 동작.
/// </summary>
public static class SceneLoader
{
    public struct SpawnData
    {
        public Vector2 position;
        public Vector2 direction;
        public bool    hasData;
    }

    private static SpawnData _pending;

    /// <summary>스폰 위치·방향을 기억하고 씬을 전환한다</summary>
    public static void LoadScene(string sceneName, Vector2 spawnPos, Vector2 spawnDir)
    {
        _pending = new SpawnData
        {
            position  = spawnPos,
            direction = spawnDir,
            hasData   = true
        };
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 저장된 스폰 데이터를 꺼낸다. 데이터가 있으면 true, 없으면 false.
    /// 호출 후 자동 삭제 (한 번만 사용).
    /// </summary>
    public static bool TryConsumePendingSpawn(out SpawnData data)
    {
        data = _pending;
        bool had = _pending.hasData;
        _pending = default;
        return had;
    }

    /// <summary>남은 스폰 데이터를 강제로 지운다 (씬 재시작 등)</summary>
    public static void ClearPendingSpawn() => _pending = default;
}

