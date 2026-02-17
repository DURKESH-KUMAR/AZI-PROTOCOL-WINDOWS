using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public bool IsMenuOpened = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!IsMenuOpened)
            {
                OpenScene4();
            }
            else
            {
                CloseMenu();
            }
        }
    }

    void CloseMenu()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        IsMenuOpened = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void OpenScene4()
    {
        SceneManager.LoadScene(4);   // Scene index 2 = Scene 3
    }
}
