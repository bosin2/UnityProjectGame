using UnityEngine;
using UnityEngine.SceneManagement; 

public class PlayerMovement : MonoBehaviour
{
    private static PlayerMovement instance; 

    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private Vector2 movement;
    private Vector2 lastDir = new Vector2(0, -1);

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        if (x != 0)
            movement = new Vector2(x, 0);
        else if (y != 0)
            movement = new Vector2(0, y);
        else
            movement = Vector2.zero;

        if (movement != Vector2.zero)
            lastDir = movement;

        anim.SetFloat("DirX", lastDir.x);
        anim.SetFloat("DirY", lastDir.y);
        anim.SetBool("IsWalking", movement != Vector2.zero);
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (PlayerPrefs.HasKey("SpawnX"))
        {
            float x = PlayerPrefs.GetFloat("SpawnX");
            float y = PlayerPrefs.GetFloat("SpawnY");
            transform.position = new Vector3(x, y, 0);

            Debug.Log($"플레이어 위치 변경: ({x}, {y})");

            PlayerPrefs.DeleteKey("SpawnX");
            PlayerPrefs.DeleteKey("SpawnY");
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}