using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private static CameraFollow instance; 
    public Transform player;
    public float offsetX = 0f;
    public float offsetY = 0f;

    void Awake() 
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // 카메라도 씬 전환해도 유지, DontDestroyOnLoad기능
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 newPos = new Vector3(
            player.position.x + offsetX,
            player.position.y + offsetY,
            transform.position.z
        );
        transform.position = newPos;
    }
}