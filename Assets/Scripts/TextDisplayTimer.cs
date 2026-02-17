using UnityEngine;
using System.Collections;

public class TextDisplayTimer : MonoBehaviour
{
    [SerializeField] private float displayTime = 5f;

    private void Start()
    {
        Invoke(nameof(HideText), displayTime);
    }

    private void HideText()
    {
        gameObject.SetActive(false);
    }
}
