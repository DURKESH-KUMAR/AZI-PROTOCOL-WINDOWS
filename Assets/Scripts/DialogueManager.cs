using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLine
{
    public string dialogueText;
    public AudioClip audioClip;
}

public class DialogueManager : MonoBehaviour
{
    public Text dialogueTextUI;
    public AudioSource audioSource;
    public List<DialogueLine> dialogueList;

    public float totalDialogueInterval = 15f; // Total time per dialogue
    public float textVisibleTime = 3f;        // Text visible duration

    void Start()
    {
        StartCoroutine(PlayDialogueLoop());
    }

    IEnumerator PlayDialogueLoop()
    {
        while (true) // Loop forever
        {
            for (int i = 0; i < dialogueList.Count; i++)
            {
                // Show text
                dialogueTextUI.text = dialogueList[i].dialogueText;

                // Play audio
                if (dialogueList[i].audioClip != null)
                {
                    audioSource.clip = dialogueList[i].audioClip;
                    audioSource.Play();
                }

                // Text visible for 3 seconds
                yield return new WaitForSeconds(textVisibleTime);

                // Hide text
                dialogueTextUI.text = "";

                // Wait remaining time (10 - 3 = 7 seconds)
                float remainingTime = totalDialogueInterval - textVisibleTime;
                if (remainingTime > 0)
                    yield return new WaitForSeconds(remainingTime);
            }
        }
    }
}
