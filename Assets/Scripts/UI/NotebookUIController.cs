using UnityEngine;

public class NotebookUIController : MonoBehaviour
{
    [SerializeField] private GameObject notebookCanvas;
    [SerializeField] private GameObject kakaoTalkWindow;
    [SerializeField] private GameObject memoWindow;
    [SerializeField] private GameObject popupRoot;

    public bool IsOpen => notebookCanvas != null && notebookCanvas.activeSelf;

    private void Awake()
    {
        if (notebookCanvas == null)
        {
            notebookCanvas = gameObject;
        }

        CloseAppWindows();
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseNotebook();
        }
    }

    public void OpenNotebook()
    {
        if (notebookCanvas != null)
        {
            notebookCanvas.SetActive(true);
        }

        CloseAppWindows();
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseNotebook()
    {
        CloseAppWindows();

        if (notebookCanvas != null)
        {
            notebookCanvas.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void OpenKakaoTalk()
    {
        ShowWindow(kakaoTalkWindow);
    }

    public void OpenMemo()
    {
        ShowWindow(memoWindow);
    }

    private void ShowWindow(GameObject window)
    {
        if (window == null)
        {
            return;
        }

        window.SetActive(true);
        window.transform.SetAsLastSibling();
    }

    private void CloseAppWindows()
    {
        if (kakaoTalkWindow != null)
        {
            kakaoTalkWindow.SetActive(false);
        }

        if (memoWindow != null)
        {
            memoWindow.SetActive(false);
        }

        if (popupRoot == null)
        {
            return;
        }

        foreach (Transform child in popupRoot.transform)
        {
            child.gameObject.SetActive(false);
        }
    }
}
