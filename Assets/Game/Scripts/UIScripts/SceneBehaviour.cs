using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneBehaviour : MonoBehaviour
{
    public void Start()
    {
        Cursor.visible=true;
        Cursor.lockState=CursorLockMode.None;
    }
    public void LoadScene()
    {
        SceneManager.LoadScene("MainMenu");
    }
    public void Quit()
    {
        Application.Quit();
    }
}
