using UnityEngine;

public class UsernameBillboard : MonoBehaviour
{
    Camera mainCam;
    void Update()
    {
        if (mainCam == null)
        {
            mainCam=FindObjectOfType<Camera>();
        }
        if(mainCam==null)
            return;
        transform.LookAt(mainCam.transform);
        transform.Rotate(Vector3.up*180);
    }
}
