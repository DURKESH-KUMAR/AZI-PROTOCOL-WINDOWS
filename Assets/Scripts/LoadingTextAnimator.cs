using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingTextAnimator : MonoBehaviour
{
    public Text loadingText;     // Assign in Inspector
    public float dotSpeed = 0.5f; // Speed of dot animation

    private string baseText = "Loading";
    private int dotCount = 0;

    void Start()
    {
        StartCoroutine(AnimateLoading());
    }

    IEnumerator AnimateLoading()
    {
        while (true)
        {
            dotCount++;

            if (dotCount > 3)
                dotCount = 0;

            loadingText.text = baseText + new string('.', dotCount);

            yield return new WaitForSeconds(dotSpeed);
        }
    }
}
