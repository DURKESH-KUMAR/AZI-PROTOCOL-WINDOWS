using UnityEngine;
using Photon.Pun;

public class PlayerManager : MonoBehaviour
{
    InputManager inputManager;
    PlayerMovement playerMovement;
    CameraManager cameraManager;
    public bool isInteracting;
    Animator animator;
    PhotonView view;
    void Awake()
    {
        view=GetComponent<PhotonView>();
        animator = GetComponent<Animator>();
        inputManager = GetComponent<InputManager>();
        playerMovement = GetComponent<PlayerMovement>();
        cameraManager = FindObjectOfType<CameraManager>();
    }
    private void Start()
    {
        if (!view.IsMine)
        {
            Destroy(GetComponentInChildren<CameraManager>().gameObject);
        }
    }
    void Update()
    {
        if(!view.IsMine)
            return;
        inputManager.HandleAllInputs();

    }
    void FixedUpdate()
    {
        if(!view.IsMine)
            return;
        playerMovement.HandleAllMovement();
    }
    void LateUpdate()
    {
        if(!view.IsMine)
            return;
        cameraManager.HandleAllCameraMovement();
        isInteracting = animator.GetBool("isInteracting");
        playerMovement.isJumping = animator.GetBool("isJumping");
        animator.SetBool("isGrounded", playerMovement.isGrounded);
    }
}
