using UnityEngine;

public class HotbarUI : MonoBehaviour
{
    public static HotbarUI Instance;

    void Awake()
    {
        Instance = this;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}