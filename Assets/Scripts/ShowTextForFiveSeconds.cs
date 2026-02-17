using System.Collections;
using UnityEngine;

public class ShowTextForFiveSeconds : MonoBehaviour
{
    public GameObject textObject;  // Drag your Text GameObject here
    public float displayTime = 5f; // Duration (default 5 seconds)

    void Start()
    {
        StartCoroutine(ShowText());
    }

    IEnumerator ShowText()
    {
        // Activate the text
        textObject.SetActive(true);

        // Wait for 5 seconds
        yield return new WaitForSeconds(displayTime);

        // Deactivate the text
        textObject.SetActive(false);
    }
}
