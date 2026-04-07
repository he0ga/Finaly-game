using UnityEngine;
using PrimeTween;

public class SystemDoor : MonoBehaviour
{
    [Header("Door Objects")]
    public GameObject Left_Door;
    public GameObject Right_Door;

    [Header("Remote Control")]
    public GameObject RemoteControll;

    [Header("Audio")]
    public AudioSource LeftDoorSound;
    public AudioSource RightDoorSound;

    [Header("UI Elements")]
    public GameObject ScriptCameras;
    public GameObject CamerasButton;
    public GameObject panelCameras;
    public GameObject navigation;

    [Header("Camera Settings")]
    public Camera mainCamera;
    public float frontPosition;
    public float leftPosition;
    public float rightPosition;

    [Header("Anim Settings")]
    public GameObject animhands;

    [Header("Light door")]
    public Light leftLight;
    public Light rightLight;


    private bool leftDoorWasPlaying = false;
    private bool rightDoorWasPlaying = false;
    private bool isAnyDoorInUse = false;
    private const float rotationThreshold = 0.1f;
    private DoorType currentActiveDoor = DoorType.None;
    private Animator anim;

    private enum DoorType { None, Left, Right }

    private void Start()
    {
        InitializeObjects();
    }

    private void InitializeObjects()
    {
        if (Left_Door != null) Left_Door.SetActive(false);
        if (Right_Door != null) Right_Door.SetActive(false);
        if (RemoteControll != null) RemoteControll.SetActive(false);
        if (ScriptCameras != null) ScriptCameras.SetActive(true);
        if (animhands != null)
        {
            anim = animhands.GetComponent<Animator>();
            anim.SetBool("isActive", false);
        }
        leftLight.color = Color.green;
        rightLight.color = Color.green;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("Main camera is not assigned!");
            return;
        }

        HandleRemoteControl();
        HandleUIElements();
        HandleDoors();
        animHands();
    }

    private void HandleRemoteControl()
    {
        float cameraRotationY = mainCamera.transform.rotation.eulerAngles.y;
        bool isFrontPosition = Mathf.Abs(Mathf.DeltaAngle(cameraRotationY, frontPosition)) < rotationThreshold;

        if (Input.GetKeyDown(KeyCode.Space) && isFrontPosition)
        {
            if (RemoteControll != null) RemoteControll.SetActive(true);
        }
        if (Input.GetKeyUp(KeyCode.Space) || !isFrontPosition)
        {
            if (RemoteControll != null) RemoteControll.SetActive(false);
        }
    }

    private void HandleUIElements()
    {
        bool remoteActive = RemoteControll != null && RemoteControll.activeSelf;
        if (ScriptCameras != null) ScriptCameras.SetActive(!remoteActive);
        if (CamerasButton != null) CamerasButton.SetActive(!remoteActive);

        if (panelCameras != null && panelCameras.activeSelf && remoteActive &&
            Mathf.Abs(Mathf.DeltaAngle(mainCamera.transform.rotation.eulerAngles.y, frontPosition)) < rotationThreshold)
        {
            panelCameras.SetActive(false);
            if (navigation != null) navigation.SetActive(true);
        }
    }

    private void HandleDoors()
    {
        bool remoteActive = RemoteControll != null && RemoteControll.activeSelf;

        if (!remoteActive)
        {
            CloseAllDoors();
            return;
        }

        // Приоритет для новой клавиши, если ни одна дверь не активна
        if (currentActiveDoor == DoorType.None)
        {
            if (Input.GetKey(KeyCode.Q))
            {
                OpenDoor(DoorType.Left);
            }
            else if (Input.GetKey(KeyCode.E))
            {
                OpenDoor(DoorType.Right);
            }
        }
        // Если уже активна какая-то дверь, реагируем только на её клавишу
        else
        {
            if (currentActiveDoor == DoorType.Left && !Input.GetKey(KeyCode.Q))
            {
                CloseDoor(DoorType.Left);
            }
            else if (currentActiveDoor == DoorType.Right && !Input.GetKey(KeyCode.E))
            {
                CloseDoor(DoorType.Right);
            }
        }
    }

    private void OpenDoor(DoorType doorType)
    {
        if (isAnyDoorInUse) return;

        switch (doorType)
        {
            case DoorType.Left:
                if (Left_Door != null && !Left_Door.activeSelf)
                {
                    Left_Door.SetActive(true);
                    if (LeftDoorSound != null && !leftDoorWasPlaying)
                    {
                        LeftDoorSound.Play();
                        leftDoorWasPlaying = true;
                    }
                    currentActiveDoor = DoorType.Left;
                    isAnyDoorInUse = true;
                    leftLight.color = Color.red;
                }
                break;

            case DoorType.Right:
                if (Right_Door != null && !Right_Door.activeSelf)
                {
                    Right_Door.SetActive(true);
                    if (RightDoorSound != null && !rightDoorWasPlaying)
                    {
                        RightDoorSound.Play();
                        rightDoorWasPlaying = true;
                    }
                    currentActiveDoor = DoorType.Right;
                    isAnyDoorInUse = true;

                    rightLight.color = Color.red;
                }
                break;
        }
    }

    private void CloseDoor(DoorType doorType)
    {
        switch (doorType)
        {
            case DoorType.Left:
                if (Left_Door != null && Left_Door.activeSelf)
                {
                    Left_Door.SetActive(false);
                    if (LeftDoorSound != null && leftDoorWasPlaying)
                    {
                        LeftDoorSound.Play();
                        leftDoorWasPlaying = false;
                        leftLight.color= Color.green;
                    }
                }
                break;

            case DoorType.Right:
                if (Right_Door != null && Right_Door.activeSelf)
                {
                    Right_Door.SetActive(false);
                    if (RightDoorSound != null && rightDoorWasPlaying)
                    {
                        RightDoorSound.Play();
                        rightDoorWasPlaying = false;
                        rightLight.color = Color.green;
                    }
                }
                break;
        }

        currentActiveDoor = DoorType.None;
        isAnyDoorInUse = false;
    }

    private void CloseAllDoors()
    {
        if (currentActiveDoor == DoorType.Left)
        {
            CloseDoor(DoorType.Left);
        }
        else if (currentActiveDoor == DoorType.Right)
        {
            CloseDoor(DoorType.Right);
        }
    }

    private void animHands()
    {
        if (anim == null) return;

        float cameraRotationY = mainCamera.transform.rotation.eulerAngles.y;
        bool isRightPosition = Mathf.Abs(Mathf.DeltaAngle(cameraRotationY, rightPosition)) < rotationThreshold;
        bool shouldActivate = isRightPosition && Input.GetKey(KeyCode.Space);

        // Активируем аниматор при необходимости
    

        // Устанавливаем параметр анимации
        anim.SetBool("isActive", shouldActivate);
        
    }

}