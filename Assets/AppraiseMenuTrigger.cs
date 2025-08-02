using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AppraiseMenuTrigger : MonoBehaviour
{
    [SerializeField] private GameObject appraiseMenu;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Button battleButton;
    [SerializeField] private Button backButton;
    
    private void Start()
    {
        SetupButtons();
    }
    
    private void Update()
    {
        HandleInput();
    }
    
    private void SetupButtons()
    {
        if (battleButton != null)
            battleButton.onClick.AddListener(OnBattleButtonPressed);
        
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonPressed);
    }
    
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && !appraiseMenu.activeSelf)
        {
            OpenAppraiseMenu();
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && appraiseMenu.activeSelf)
        {
            OnBackButtonPressed();
        }
    }
    
    private void OpenAppraiseMenu()
    {
        if (playerController == null || !playerController.IsHoldingObject())
        {
            Debug.Log("[AppraiseMenu] No stick to appraise!");
            return;
        }
        
        GameObject heldObject = playerController.GetHeldObject();
        Stick stick = heldObject.GetComponent<Stick>();
        
        if (stick == null)
        {
            Debug.Log("[AppraiseMenu] Held object is not a stick!");
            return;
        }
        
        DisplayStickStats(stick);
        appraiseMenu.SetActive(true);
        playerController.enabled = false;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void CloseAppraiseMenu()
    {
        appraiseMenu.SetActive(false);
        playerController.enabled = true;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void OnBattleButtonPressed()
    {
        Debug.Log("[AppraiseMenu] Battle button pressed!");
        
        if (GameModeManager.Instance != null)
        {
            CloseAppraiseMenu();
            GameModeManager.Instance.StartBattle();
        }
        else
        {
            Debug.LogError("[AppraiseMenu] GameModeManager not found!");
        }
    }
    
    public void OnBackButtonPressed()
    {
        Debug.Log("[AppraiseMenu] Back button pressed!");
        CloseAppraiseMenu();
    }
    
    private void DisplayStickStats(Stick stick)
    {
        if (statsText == null)
        {
            Debug.LogWarning("[AppraiseMenu] Stats text component not assigned!");
            return;
        }
        
        if (!stick.HasStatsGenerated())
        {
            statsText.text = "This stick hasn't been analyzed yet.\nPick it up first to generate stats.";
            return;
        }
        
        string statsDescription = stick.GetStatsDescription();
        statsText.text = statsDescription;
    }
}
