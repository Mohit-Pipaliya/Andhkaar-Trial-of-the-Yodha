using UnityEngine;
using UnityEngine.InputSystem;

public class FlyAndCollectMechanic : MonoBehaviour
{
    [Header("Trigger Locations")]
    [Tooltip("Pehla trigger jaha se player fly karega")]
    public Transform trigger1;
    [Tooltip("Dusra trigger jaha player udke jayega")]
    public Transform trigger2;
    [Tooltip("Kitni dur se trigger kaam karega")]
    public float interactionDistance = 3f;

    [Header("Flying Settings")]
    [Tooltip("Player ke udne ki speed")]
    public float flySpeed = 10f;
    [Tooltip("Agar sach (true) hai, toh player Trigger 2 par pahuche ke baad move nahi kar payega jab tak object collect na kare")]
    public bool freezePlayerAtTrigger2 = true;

    [Header("Object & Light Setup")]
    [Tooltip("Trigger 2 pe jo Point Light chalu karni hai, use yaha dale")]
    public Light pointLight;
    [Tooltip("Jo object collect karna hai (jisme aapki khud ki script hai), use yaha dale")]
    public GameObject collectibleObject;

    [Header("UI")]
    [Tooltip("Yaha apna custom Canvas UI text (GameObject) dale jisme 'Press F to Fly' likha ho")]
    public GameObject pressFUI;

    private Transform playerTransform;
    private CharacterController charController;
    private PlayerController playerCtrl;
    private bool isNearTrigger1 = false;
    
    // States: 
    // Idle (Trigger 1 pe hai) -> FlyingTo2 -> AtTrigger2 -> FlyingTo1
    private enum MechanicState { Idle, FlyingTo2, AtTrigger2, FlyingTo1 }
    private MechanicState currentState = MechanicState.Idle;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            charController = player.GetComponent<CharacterController>();
            playerCtrl = player.GetComponent<PlayerController>();
        }

        if (pointLight != null) pointLight.enabled = false;
        if (pressFUI != null) pressFUI.SetActive(false);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        switch (currentState)
        {
            case MechanicState.Idle:
                HandleIdleState();
                break;
            case MechanicState.FlyingTo2:
                HandleFlyingState(trigger2.position, MechanicState.AtTrigger2);
                break;
            case MechanicState.AtTrigger2:
                HandleAtTrigger2State();
                break;
            case MechanicState.FlyingTo1:
                HandleFlyingState(trigger1.position, MechanicState.Idle);
                break;
        }
    }

    private void HandleIdleState()
    {
        if (trigger1 == null) return;

        bool near = false;
        Collider triggerCol = trigger1.GetComponent<Collider>();
        if (triggerCol != null)
        {
            near = triggerCol.bounds.Contains(playerTransform.position);
        }
        else
        {
            float dist = Vector3.Distance(playerTransform.position, trigger1.position);
            near = dist <= interactionDistance;
        }

        if (near && !isNearTrigger1) 
        {
            if (pointLight != null) pointLight.enabled = true;
        }
        else if (!near && isNearTrigger1 && currentState == MechanicState.Idle)
        {
            if (pointLight != null) pointLight.enabled = false;
        }
        
        isNearTrigger1 = near;

        if (pressFUI != null) pressFUI.SetActive(isNearTrigger1);

        if (isNearTrigger1 && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            isNearTrigger1 = false; 
            
            if (pressFUI != null) pressFUI.SetActive(false);
            
            if (charController != null) charController.enabled = false; 
            if (playerCtrl != null) playerCtrl.SetLampFreeze(true);
            
            currentState = MechanicState.FlyingTo2;
        }
    }

    private void HandleFlyingState(Vector3 targetPos, MechanicState nextState)
    {
        playerTransform.position = Vector3.MoveTowards(playerTransform.position, targetPos, flySpeed * Time.deltaTime);

        bool reachedTarget = false;

        // Force exactly to the center of the trigger target
        if (Vector3.Distance(playerTransform.position, targetPos) < 0.1f)
        {
            reachedTarget = true;
        }

        if (reachedTarget)
        {
            currentState = nextState;

            if (nextState == MechanicState.AtTrigger2)
            {
                // Agar freeze on hai, to player ko move karne se rok kar rakho (charController off rakho)
                if (!freezePlayerAtTrigger2) 
                {
                    if (charController != null) charController.enabled = true;
                }
                if (playerCtrl != null) playerCtrl.SetLampFreeze(false);
            }
            else if (nextState == MechanicState.Idle)
            {
                if (charController != null) charController.enabled = true;
                if (playerCtrl != null) playerCtrl.SetLampFreeze(false);
            }
        }
    }

    private void HandleAtTrigger2State()
    {
        if (collectibleObject == null || !collectibleObject.activeInHierarchy)
        {
            if (pointLight != null) pointLight.enabled = false;

            if (charController != null) charController.enabled = false;
            if (playerCtrl != null) playerCtrl.SetLampFreeze(true);
            
            currentState = MechanicState.FlyingTo1;
        }
    }

    private void OnGUI()
    {
        if (pressFUI != null) return;

        if (currentState == MechanicState.Idle && isNearTrigger1)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 50; 
            style.fontStyle = FontStyle.Bold; 
            style.alignment = TextAnchor.MiddleCenter;
            
            Rect rect = new Rect(0, Screen.height - 150, Screen.width, 100);

            style.normal.textColor = new Color(0, 0, 0, 0.7f); 
            Rect shadowRect = new Rect(rect.x + 3, rect.y + 3, rect.width, rect.height);
            GUI.Label(shadowRect, "Press F to Fly", style);

            style.normal.textColor = Color.white;
            GUI.Label(rect, "Press F to Fly", style);
        }
    }
}
