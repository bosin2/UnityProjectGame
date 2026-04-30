using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void OnStartGame()
    {
        SceneManager.LoadScene("Intro");
    }

    public void OnSettings()
    {
        Debug.Log("클릭");
    }

    public void OnQuit()
    {
        Application.Quit();
    }
}