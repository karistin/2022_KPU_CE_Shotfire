using UnityEngine;
using Cinemachine;
using Photon.Pun;
using Photon.Realtime;


#region 필요한 컴포넌트
[RequireComponent(typeof(PlayerInput))]
#endregion

public class PlayerAttack : MonoBehaviourPunCallbacks
{
    PlayerInput playerInput;

    #region 전역 변수
    [SerializeField] Transform m_cameraHolder; // 카메라 거치대 자리
    [Header("화면 이동 속도")]
    [SerializeField] float m_screenXSpeed = 1f; // X축 화면 이동속도
    [SerializeField] float m_screenYSpeed = 1f; // Y축 화면 이동속도
    [SerializeField] float m_screenNormalSpeed = 1f;    // 평상시 화면 이동속도 계수
    [SerializeField] float m_screenZoomSpeed = 0.5f;    // 줌화면 이동속도 계수
    [Header("화면 최대 각도")]
    [SerializeField] float m_minScreenAngle = 80f;  // 화면 최대 아래 각도
    [SerializeField] float m_maxScreenAngle = 80f;  //화면 최대 위 각도
    [Header("카메라 거치대 위치")] 
    GameObject m_virtualMainCamera;    // 메인 카메라 자리
    CinemachineVirtualCamera m_mainCam;    // 메인 카메라의 가상카메라
    CinemachineVirtualCamera m_zoomCam;    // 줌 카메라의 가상카메라
    Vector3 curCamHolderLocalPosition = Vector3.zero;   // 현재 카메라 홀더의 로컬 위치
    [SerializeField] float m_idleCamHolderHeight = 1.8f;    // 평소 상태 카메라 높이
    [SerializeField] float m_zoomCamHolderHeught = 1.55f;   // 줌 상태 카메라 높이
    [Header("무기")] 
    [SerializeField] Weapon[] weaponArray = new Weapon[2];   // 1~4번 슬릇에 사용할 무기 배열
    #endregion

    #region 전역 동작 변수
    // 화면
    float curScreenSpeed;   // 현재 화면 회전 속도
    bool isZoomMode = false;    // 줌 상태 여부
    // 무기
    int curWeapon = 0;   // 현재 들고 있는 무기의 배열 번호
    #endregion
#region 콜백함수
    private void Start()
    {
        // 하이어라키 상의 무기를 배열에 세팅
        foreach(Weapon weapon in weaponArray)
        {
            weapon.gameObject.SetActive(false);
        }
        weaponArray[curWeapon].gameObject.SetActive(true);

        if(!photonView.IsMine) return ; // 네트워크 통제 구역

        curScreenSpeed = m_screenNormalSpeed;
        curCamHolderLocalPosition.y = m_idleCamHolderHeight;
        m_cameraHolder.localPosition = curCamHolderLocalPosition;
        playerInput = GetComponent<PlayerInput>();

        m_virtualMainCamera = GameObject.Find("Camera Main");   
        m_mainCam = m_virtualMainCamera.GetComponent<CinemachineVirtualCamera>();   
        m_zoomCam = GameObject.Find("Camera Zoom").GetComponent<CinemachineVirtualCamera>();
        m_mainCam.Follow = m_zoomCam.Follow = transform.Find("Camera Holder");
    }
    private void Update()
    {
        if(!photonView.IsMine) return ; // 네트워크 통제 구역

        if(m_virtualMainCamera != null) RotateScreen();
        isZoomMode = playerInput.zoom;

        if(playerInput.fire) weaponArray[curWeapon].Fire();
        else weaponArray[curWeapon].Detached();
        
        if(playerInput.reload) 
        {
            weaponArray[curWeapon].Reload();
            playerInput.reload = false;
        }
        if(playerInput.fireMode) 
        {
            weaponArray[curWeapon].ChangeFireMode();
            playerInput.fireMode = false;
        }

        if(playerInput.zoom) weaponArray[curWeapon].SetFireState(false);
        else weaponArray[curWeapon].SetFireState(true);

        ChangeWeaponCommand();
    }

    private void FixedUpdate() {
        if(!photonView.IsMine) return ; // 네트워크 통제 구역
        if(m_virtualMainCamera != null) SetScreenMode();
    }
#endregion
#region 함수
    // 화면 회전
    private void RotateScreen()
    {
        m_cameraHolder.rotation *= Quaternion.AngleAxis(playerInput.look.x * m_screenXSpeed * curScreenSpeed, Vector3.up);
        m_cameraHolder.rotation *= Quaternion.AngleAxis(playerInput.look.y * m_screenYSpeed * curScreenSpeed, Vector3.left);

        var angles = m_cameraHolder.localEulerAngles;
        var angle = m_cameraHolder.localEulerAngles.x;

        if (180 < angle && angle < 360 - m_minScreenAngle)
        {
            angles.x = 360 - m_minScreenAngle;
        }
        else if (m_maxScreenAngle < angle && angle < 180)
        {
            angles.x = m_maxScreenAngle;
        }

        angles.z = 0;

        m_cameraHolder.localEulerAngles = angles;
    }

    // 줌 인/아웃 처리
    private void SetScreenMode()
    {
        m_virtualMainCamera.SetActive(!isZoomMode);

        if (!isZoomMode)    // idle 상태
        {
            curScreenSpeed = m_screenNormalSpeed;

            curCamHolderLocalPosition.y = m_idleCamHolderHeight;
            m_cameraHolder.localPosition = curCamHolderLocalPosition;
        }
        else    // zoom 상태
        {
           curScreenSpeed = m_screenZoomSpeed;

            curCamHolderLocalPosition.y = m_zoomCamHolderHeught;
            m_cameraHolder.localPosition = curCamHolderLocalPosition;
        }
    }
    private void ChangeWeaponCommand()
    {
        if(Input.GetButtonDown("Weapon1")) ChangeWeapon(0);
        else if(Input.GetButtonDown("Weapon2")) ChangeWeapon(1);
        else if(Input.GetButtonDown("Weapon3")) ChangeWeapon(2);
        else if(Input.GetButtonDown("Weapon4")) ChangeWeapon(3);
    }
    private void ChangeWeapon(int _newWeaponIndex)
    {
        weaponArray[curWeapon].gameObject.SetActive(false);
        curWeapon = _newWeaponIndex;
        weaponArray[curWeapon].gameObject.SetActive(true);

        weaponArray[curWeapon].UpdateUI();
    }
#endregion
}