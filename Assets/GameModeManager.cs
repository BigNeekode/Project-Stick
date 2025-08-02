using UnityEngine;

public enum GameMode
{
    Exploration,
    Battle
}

public class GameModeManager : MonoBehaviour
{
    [Header("Game Mode Settings")]
    [SerializeField] private GameMode currentGameMode = GameMode.Exploration;
    
    [Header("Scene References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform playerBattlePosition;
    [SerializeField] private Transform enemyBattlePosition;
    
    [Header("Enemy Settings")]
    [SerializeField] private GameObject enemyPrefab;
    
    [Header("UI References")]
    [SerializeField] private GameObject explorationUI;
    [SerializeField] private GameObject battleUI;
    
    public static GameModeManager Instance { get; private set; }
    
    private Vector3 playerExplorationPosition;
    private Quaternion playerExplorationRotation;
    private GameObject currentEnemyInstance;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        SetGameMode(GameMode.Exploration);
    }
    
    public void SetGameMode(GameMode newMode)
    {
        currentGameMode = newMode;
        
        switch (newMode)
        {
            case GameMode.Exploration:
                EnterExplorationMode();
                break;
            case GameMode.Battle:
                EnterBattleMode();
                break;
        }
    }
    
    public void StartBattle()
    {
        if (!ValidateBattleRequirements()) return;
        
        SavePlayerExplorationState();
        SetGameMode(GameMode.Battle);
    }
    
    public void EndBattle()
    {
        SetGameMode(GameMode.Exploration);
    }
    
    private bool ValidateBattleRequirements()
    {
        if (playerController == null)
        {
            Debug.LogError("[GameModeManager] PlayerController not assigned!");
            return false;
        }
        
        if (playerBattlePosition == null)
        {
            Debug.LogError("[GameModeManager] Player battle position not assigned!");
            return false;
        }
        
        if (enemyBattlePosition == null)
        {
            Debug.LogError("[GameModeManager] Enemy battle position not assigned!");
            return false;
        }
        
        if (BattleManager.Instance == null)
        {
            Debug.LogError("[GameModeManager] BattleManager instance not found!");
            return false;
        }
        
        if (enemyPrefab == null)
        {
            Debug.LogError("[GameModeManager] Enemy prefab not assigned!");
            return false;
        }

        if (!playerController.IsHoldingObject())
        {
            Debug.LogWarning("[GameModeManager] Player must be holding a stick to battle!");
            return false;
        }

        GameObject heldObject = playerController.GetHeldObject();
        Stick playerStick = heldObject.GetComponent<Stick>();

        if (playerStick == null || !playerStick.HasStatsGenerated())
        {
            Debug.LogWarning("[GameModeManager] Player's stick must have generated stats!");
            return false;
        }

        Debug.Log("[GameModeManager] All battle requirements validated successfully!");
        return true;
    }
    
    private void SavePlayerExplorationState()
    {
        if (playerController != null)
        {
            playerExplorationPosition = playerController.transform.position;
            playerExplorationRotation = playerController.transform.rotation;
        }
    }
    
    private void EnterExplorationMode()
    {
        Debug.Log("[GameModeManager] Entering Exploration Mode");
        
        if (playerController != null)
        {
            playerController.enabled = true;
            
            if (playerExplorationPosition != Vector3.zero)
            {
                playerController.transform.position = playerExplorationPosition;
                playerController.transform.rotation = playerExplorationRotation;
            }
        }
        
        // Destroy current enemy instance
        DestroyCurrentEnemy();
        
        if (BattleManager.Instance != null)
            BattleManager.Instance.gameObject.SetActive(false);
        
        SetUIState(true, false);
    }
    
    private void EnterBattleMode()
    {
        Debug.Log("[GameModeManager] Entering Battle Mode");
        
        if (playerController != null)
        {
            playerController.enabled = false;
            
            if (playerBattlePosition != null)
            {
                Debug.Log($"[GameModeManager] Moving player from {playerController.transform.position} to {playerBattlePosition.position}");
                playerController.transform.position = playerBattlePosition.position;
                playerController.transform.rotation = playerBattlePosition.rotation;
            }
            else
            {
                Debug.LogWarning("[GameModeManager] Player battle position not assigned!");
            }
        }
        
        // Instantiate enemy at battle position
        SpawnEnemy();
        
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.gameObject.SetActive(true);
            BattleManager.Instance.StartBattle();
        }
        else
        {
            Debug.LogError("[GameModeManager] BattleManager instance not found!");
        }
        
        SetUIState(false, true);
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null || enemyBattlePosition == null)
        {
            Debug.LogError("[GameModeManager] Cannot spawn enemy - prefab or position missing!");
            return;
        }
        
        // Destroy any existing enemy
        DestroyCurrentEnemy();
        
        // Instantiate new enemy
        currentEnemyInstance = Instantiate(enemyPrefab, enemyBattlePosition.position, enemyBattlePosition.rotation);
        
        Debug.Log($"[GameModeManager] Enemy spawned at {enemyBattlePosition.position}");
    }

    private void DestroyCurrentEnemy()
    {
        if (currentEnemyInstance != null)
        {
            Debug.Log("[GameModeManager] Destroying current enemy instance");
            Destroy(currentEnemyInstance);
            currentEnemyInstance = null;
        }
    }

    public GameObject GetCurrentEnemy()
    {
        return currentEnemyInstance;
    }
    
    private void SetUIState(bool explorationActive, bool battleActive)
    {
        if (explorationUI != null)
            explorationUI.SetActive(explorationActive);
        
        if (battleUI != null)
            battleUI.SetActive(battleActive);
    }
    
    public GameMode GetCurrentGameMode()
    {
        return currentGameMode;
    }
    
    public bool IsInBattle()
    {
        return currentGameMode == GameMode.Battle;
    }
    
    public Transform GetPlayerBattlePosition()
    {
        return playerBattlePosition;
    }
    
    public Transform GetEnemyBattlePosition()
    {
        return enemyBattlePosition;
    }
}
