using UnityEngine;
using System.Collections;

public enum BattleState
{
    Setup,
    PlayerTurn,
    EnemyTurn,
    PlayerAction,
    EnemyAction,
    Victory,
    Defeat
}

public enum BattleAction
{
    Attack,
    SpecialAttack,
    Defend
}

[System.Serializable]
public class BattleEntity
{
    public string name;
    public float maxHealth;
    public float currentHealth;
    public Stick stick;
    public Transform rootTransform;
    public Animator animator;
    public bool isDefending;
    
    public bool IsAlive => currentHealth > 0;
    public float HealthPercentage => currentHealth / maxHealth;
}

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    
    [Header("Battle Settings")]
    [SerializeField] private float battleDistance = 2f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dodgeWindow = 0.5f;
    
    [Header("Battle Entities")]
    [SerializeField] private BattleEntity player;
    [SerializeField] private BattleEntity enemy;
    
    [Header("Battle Positions")]
    [SerializeField] private Transform playerBattleRoot;
    [SerializeField] private Transform enemyBattleRoot;
    
    [Header("UI References")]
    [SerializeField] private GameObject battleUI;
    
    private BattleState currentState;
    private bool dodgeWindowActive;
    private bool playerInputEnabled;
    private BattleAction currentPlayerAction;
    private BattleAction currentEnemyAction;
    
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
        gameObject.SetActive(false);
    }
    
    private void Update()
    {
        HandleBattleInput();
    }
    
    public void StartBattle()
    {
        // Find battle positions from GameModeManager if not set
        if (playerBattleRoot == null || enemyBattleRoot == null)
        {
            InitializeBattlePositions();
        }
        
        SetupBattle();
        currentState = BattleState.Setup;
        StartCoroutine(BattleLoop());
    }
    
    private void InitializeBattlePositions()
    {
        if (GameModeManager.Instance != null)
        {
            // Get battle positions from GameModeManager
            Transform playerPos = GameModeManager.Instance.GetPlayerBattlePosition();
            Transform enemyPos = GameModeManager.Instance.GetEnemyBattlePosition();
            
            if (playerPos != null) playerBattleRoot = playerPos;
            if (enemyPos != null) enemyBattleRoot = enemyPos;
            
            Debug.Log($"[BattleManager] Battle positions initialized - Player: {playerBattleRoot != null}, Enemy: {enemyBattleRoot != null}");
        }
    }
    
    private void SetupBattle()
    {
        Debug.Log("[BattleManager] Setting up battle...");
        
        SetupPlayer();
        SetupEnemy();
        
        if (player.stick == null)
        {
            Debug.LogError("[BattleManager] Player stick is null! Cannot start battle.");
            EndBattle();
            return;
        }
        
        if (enemy.stick == null)
        {
            Debug.LogError("[BattleManager] Enemy stick is null! Cannot start battle.");
            EndBattle();
            return;
        }
        
        Debug.Log("[BattleManager] Battle Started!");
        Debug.Log($"Player: {player.name} (HP: {player.currentHealth}) with {player.stick.GetStickType()} stick");
        Debug.Log($"Enemy: {enemy.name} (HP: {enemy.currentHealth}) with {enemy.stick.GetStickType()} stick");
    }
    
    private void SetupPlayer()
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null) return;
        
        GameObject heldStick = playerController.GetHeldObject();
        if (heldStick == null) return;
        
        player.name = "Player";
        player.maxHealth = 100f;
        player.currentHealth = player.maxHealth;
        player.stick = heldStick.GetComponent<Stick>();
        player.rootTransform = playerController.transform; // Use actual player transform
        player.animator = playerController.GetComponent<Animator>();
        player.isDefending = false;
        
        Debug.Log($"[BattleManager] Player setup - Transform: {player.rootTransform.name} at {player.rootTransform.position}");
    }
    
    private void SetupEnemy()
    {
        // Get the enemy instance from GameModeManager
        GameObject enemyInstance = GameModeManager.Instance?.GetCurrentEnemy();
        
        if (enemyInstance == null)
        {
            Debug.LogError("[BattleManager] No enemy instance found!");
            EndBattle();
            return;
        }
        
        enemy.name = "Enemy";
        enemy.maxHealth = 100f;
        enemy.currentHealth = enemy.maxHealth;
        enemy.rootTransform = enemyInstance.transform; // Use actual enemy transform
        enemy.animator = enemyInstance.GetComponent<Animator>();
        enemy.isDefending = false;
        
        // Check if enemy already has a stick component, if not create one
        enemy.stick = enemyInstance.GetComponent<Stick>();
        if (enemy.stick == null)
        {
            enemy.stick = enemyInstance.AddComponent<Stick>();
            enemy.stick.GenerateStats();
        }
        
        Debug.Log($"[BattleManager] Enemy setup complete - HP: {enemy.currentHealth}, Stick: {enemy.stick.GetStickType()}");
        Debug.Log($"[BattleManager] Enemy Transform: {enemy.rootTransform.name} at {enemy.rootTransform.position}");
    }
    
    private void GenerateEnemyStick()
    {
        GameObject enemyStickObject = new GameObject("EnemyStick");
        enemy.stick = enemyStickObject.AddComponent<Stick>();
        
        // Randomize enemy stick type
        StickType[] stickTypes = { StickType.Short, StickType.Medium, StickType.Long };
        StickType randomType = stickTypes[Random.Range(0, stickTypes.Length)];
        
        // Create a temporary stick with the desired type
        // Since stickType is private, we'll use the default and generate stats anyway
        enemy.stick.GenerateStats();
        
        Debug.Log($"[BattleManager] Generated enemy stick with stats: Power: {enemy.stick.GetStats().power}, Speed: {enemy.stick.GetStats().speed}");
    }
    
    private IEnumerator BattleLoop()
    {
        while (currentState != BattleState.Victory && currentState != BattleState.Defeat)
        {
            switch (currentState)
            {
                case BattleState.Setup:
                    yield return StartCoroutine(DetermineTurnOrder());
                    break;
                case BattleState.PlayerTurn:
                    yield return StartCoroutine(HandlePlayerTurn());
                    break;
                case BattleState.EnemyTurn:
                    yield return StartCoroutine(HandleEnemyTurn());
                    break;
                case BattleState.PlayerAction:
                    yield return StartCoroutine(ExecutePlayerAction());
                    break;
                case BattleState.EnemyAction:
                    yield return StartCoroutine(ExecuteEnemyAction());
                    break;
            }
            
            CheckBattleEnd();
            yield return null;
        }
        
        EndBattle();
    }
    
    private IEnumerator DetermineTurnOrder()
    {
        float playerSpeed = player.stick.GetStats().speed;
        float enemySpeed = enemy.stick.GetStats().speed;
        
        if (playerSpeed >= enemySpeed)
        {
            Debug.Log("[BattleManager] Player goes first!");
            currentState = BattleState.PlayerTurn;
        }
        else
        {
            Debug.Log("[BattleManager] Enemy goes first!");
            currentState = BattleState.EnemyTurn;
        }
        
        yield return new WaitForSeconds(1f);
    }
    
    private IEnumerator HandlePlayerTurn()
    {
        Debug.Log("[BattleManager] Player's turn - Choose action (Q: Attack, W: Special, E: Defend)");
        playerInputEnabled = true;
        
        while (currentState == BattleState.PlayerTurn)
        {
            yield return null;
        }
        
        playerInputEnabled = false;
    }
    
    private IEnumerator HandleEnemyTurn()
    {
        Debug.Log("[BattleManager] Enemy's turn");
        yield return new WaitForSeconds(1f);
        
        // Random enemy action with weighted probabilities
        currentEnemyAction = GetRandomEnemyAction();
        
        switch (currentEnemyAction)
        {
            case BattleAction.Attack:
                Debug.Log("[BattleManager] Enemy chooses: Attack!");
                break;
            case BattleAction.SpecialAttack:
                Debug.Log("[BattleManager] Enemy chooses: Special Attack!");
                break;
            case BattleAction.Defend:
                Debug.Log("[BattleManager] Enemy chooses: Defend!");
                enemy.isDefending = true;
                // If enemy defends, skip to player turn
                currentState = BattleState.PlayerTurn;
                yield break;
        }
        
        currentState = BattleState.EnemyAction;
    }
    
    private BattleAction GetRandomEnemyAction()
    {
        // Weighted random selection for more interesting AI
        float randomValue = Random.Range(0f, 100f);
        
        // 50% chance to attack, 30% special attack, 20% defend
        if (randomValue < 50f)
            return BattleAction.Attack;
        else if (randomValue < 80f)
            return BattleAction.SpecialAttack;
        else
            return BattleAction.Defend;
    }
    
    private IEnumerator ExecutePlayerAction()
    {
        yield return StartCoroutine(ExecuteAction(player, enemy, currentPlayerAction));
        currentState = BattleState.EnemyTurn;
    }
    
    private IEnumerator ExecuteEnemyAction()
    {
        yield return StartCoroutine(ExecuteAction(enemy, player, currentEnemyAction));
        currentState = BattleState.PlayerTurn;
    }
    
    private IEnumerator ExecuteAction(BattleEntity attacker, BattleEntity target, BattleAction action)
    {
        // Move to attack position (except for defend)
        if (action != BattleAction.Defend)
        {
            yield return StartCoroutine(MoveToAttackPosition(attacker, target));
        }
        
        // Execute the specific action
        switch (action)
        {
            case BattleAction.Attack:
                yield return StartCoroutine(PerformAttack(attacker, target, 1.0f)); // Normal damage
                break;
            case BattleAction.SpecialAttack:
                yield return StartCoroutine(PerformSpecialAttack(attacker, target));
                break;
            case BattleAction.Defend:
                yield return StartCoroutine(PerformDefend(attacker));
                break;
        }
        
        // Return to original position (except for defend)
        if (action != BattleAction.Defend)
        {
            yield return StartCoroutine(ReturnToPosition(attacker));
        }
        
        // Reset defending state after action
        attacker.isDefending = false;
    }
    
    private IEnumerator MoveToAttackPosition(BattleEntity attacker, BattleEntity target)
    {
        Vector3 startPos = attacker.rootTransform.position;
        Vector3 direction = (target.rootTransform.position - attacker.rootTransform.position).normalized;
        Vector3 targetPos = target.rootTransform.position - direction * battleDistance;
        
        Debug.Log($"[BattleManager] {attacker.name} moving from {startPos} to {targetPos} (distance: {Vector3.Distance(startPos, targetPos)})");
        
        float elapsed = 0f;
        float totalDistance = Vector3.Distance(startPos, targetPos);
        float duration = totalDistance / moveSpeed;
        
        // Avoid division by zero and ensure minimum movement time
        if (duration <= 0f)
        {
            duration = 0.1f;
        }
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            attacker.rootTransform.position = Vector3.Lerp(startPos, targetPos, progress);
            yield return null;
        }
        
        attacker.rootTransform.position = targetPos;
        Debug.Log($"[BattleManager] {attacker.name} reached attack position at {targetPos}");
    }
    
    private IEnumerator PerformAttack(BattleEntity attacker, BattleEntity target, float damageMultiplier = 1.0f)
    {
        Debug.Log($"[BattleManager] {attacker.name} attacks {target.name}!");
        
        // Start dodge window
        if (target == player)
        {
            StartDodgeWindow();
        }
        
        // Wait for attack animation timing
        yield return new WaitForSeconds(0.5f);
        
        // Check if attack was dodged
        bool dodged = dodgeWindowActive && target == player && Input.GetKey(KeyCode.Space);
        
        if (dodged)
        {
            Debug.Log($"[BattleManager] {target.name} dodged the attack!");
        }
        else
        {
            float damage = CalculateDamage(attacker, target, damageMultiplier);
            ApplyDamage(target, damage);
        }
        
        // End dodge window
        EndDodgeWindow();
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator PerformSpecialAttack(BattleEntity attacker, BattleEntity target)
    {
        Debug.Log($"[BattleManager] {attacker.name} uses Special Attack on {target.name}!");
        
        // Special attacks have longer wind-up but more damage
        yield return new WaitForSeconds(0.3f);
        
        // Start dodge window (slightly longer for special attacks)
        if (target == player)
        {
            StartDodgeWindow();
        }
        
        // Wait for special attack animation timing
        yield return new WaitForSeconds(0.7f);
        
        // Check if attack was dodged
        bool dodged = dodgeWindowActive && target == player && Input.GetKey(KeyCode.Space);
        
        if (dodged)
        {
            Debug.Log($"[BattleManager] {target.name} dodged the special attack!");
        }
        else
        {
            // Special attacks deal 1.5x damage
            float damage = CalculateDamage(attacker, target, 1.5f);
            ApplyDamage(target, damage);
        }
        
        // End dodge window
        EndDodgeWindow();
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator PerformDefend(BattleEntity defender)
    {
        Debug.Log($"[BattleManager] {defender.name} takes a defensive stance!");
        
        // Defender is already set to defending state in the action selection
        // This just provides visual feedback
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"[BattleManager] {defender.name} is ready to block incoming attacks!");
    }
    
    private IEnumerator ReturnToPosition(BattleEntity entity)
    {
        Vector3 startPos = entity.rootTransform.position;
        Vector3 originalPos;
        
        // Determine the correct return position
        if (entity == player)
        {
            originalPos = playerBattleRoot != null ? playerBattleRoot.position : startPos;
        }
        else
        {
            originalPos = enemyBattleRoot != null ? enemyBattleRoot.position : startPos;
        }
        
        Debug.Log($"[BattleManager] {entity.name} returning from {startPos} to {originalPos}");
        
        float elapsed = 0f;
        float duration = Vector3.Distance(startPos, originalPos) / moveSpeed;
        
        // Avoid division by zero
        if (duration <= 0f)
        {
            entity.rootTransform.position = originalPos;
            yield break;
        }
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            entity.rootTransform.position = Vector3.Lerp(startPos, originalPos, progress);
            yield return null;
        }
        
        entity.rootTransform.position = originalPos;
    }
    
    private void StartDodgeWindow()
    {
        dodgeWindowActive = true;
        Debug.Log("[BattleManager] Dodge window opened! Press SPACE to dodge!");
        
        StartCoroutine(DodgeWindowTimer());
    }
    
    private IEnumerator DodgeWindowTimer()
    {
        yield return new WaitForSeconds(dodgeWindow);
        EndDodgeWindow();
    }
    
    private void EndDodgeWindow()
    {
        dodgeWindowActive = false;
    }
    
    private float CalculateDamage(BattleEntity attacker, BattleEntity target, float damageMultiplier = 1.0f)
    {
        float baseDamage = attacker.stick.GetStats().power;
        float damage = baseDamage * damageMultiplier;
        
        if (target.isDefending)
        {
            damage *= 0.5f; // 50% damage reduction when defending
            Debug.Log($"[BattleManager] {target.name} is defending! Damage reduced to {damage}");
        }
        
        return damage;
    }
    
    private void ApplyDamage(BattleEntity target, float damage)
    {
        target.currentHealth = Mathf.Max(0, target.currentHealth - damage);
        Debug.Log($"[BattleManager] {target.name} takes {damage} damage! HP: {target.currentHealth}/{target.maxHealth}");
    }
    
    private void HandleBattleInput()
    {
        if (!playerInputEnabled) return;
        
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("[BattleManager] Player chooses Attack!");
            currentPlayerAction = BattleAction.Attack;
            player.isDefending = false;
            currentState = BattleState.PlayerAction;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("[BattleManager] Player chooses Special Attack!");
            currentPlayerAction = BattleAction.SpecialAttack;
            player.isDefending = false;
            currentState = BattleState.PlayerAction;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[BattleManager] Player chooses Defend!");
            currentPlayerAction = BattleAction.Defend;
            player.isDefending = true;
            currentState = BattleState.EnemyTurn;
        }
    }
    
    private void CheckBattleEnd()
    {
        if (!player.IsAlive)
        {
            currentState = BattleState.Defeat;
            Debug.Log("[BattleManager] Player defeated!");
        }
        else if (!enemy.IsAlive)
        {
            currentState = BattleState.Victory;
            Debug.Log("[BattleManager] Enemy defeated! Victory!");
        }
    }
    
    private void EndBattle()
    {
        Debug.Log("[BattleManager] Battle ended!");
        
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.EndBattle();
        }
    }
}
