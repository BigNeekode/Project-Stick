using UnityEngine;

[System.Serializable]
public class StickStats
{
    public float power;
    public float speed;
    public StickGrade powerGrade;
    public StickGrade speedGrade;
    public StickGrade overallGrade;
    
    public StickStats(float power, float speed, StickGrade powerGrade, StickGrade speedGrade, StickGrade overallGrade)
    {
        this.power = power;
        this.speed = speed;
        this.powerGrade = powerGrade;
        this.speedGrade = speedGrade;
        this.overallGrade = overallGrade;
    }
}

public enum StickGrade
{
    D, C, B, A, S
}

[System.Serializable]
public class StickTypeRange
{
    [Header("Power Range")]
    [SerializeField] public Vector2 powerRange = Vector2.zero;
    
    [Header("Speed Range")]
    [SerializeField] public Vector2 speedRange = Vector2.zero;
}

public enum StickType
{
    Short,
    Medium,
    Long
}

public class Stick : MonoBehaviour
{
    [Header("Stick Configuration")]
    [SerializeField] private StickType stickType = StickType.Medium;
    
    [Header("Short Stick Stats")]
    [SerializeField] private StickTypeRange shortStickRange = new StickTypeRange 
    { 
        powerRange = new Vector2(15f, 25f), 
        speedRange = new Vector2(80f, 100f) 
    };
    
    [Header("Medium Stick Stats")]
    [SerializeField] private StickTypeRange mediumStickRange = new StickTypeRange 
    { 
        powerRange = new Vector2(30f, 50f), 
        speedRange = new Vector2(50f, 70f) 
    };
    
    [Header("Long Stick Stats")]
    [SerializeField] private StickTypeRange longStickRange = new StickTypeRange 
    { 
        powerRange = new Vector2(60f, 80f), 
        speedRange = new Vector2(20f, 40f) 
    };
    
    [Header("Generated Stats")]
    [SerializeField] private bool statsGenerated = false;
    [SerializeField] private StickStats currentStats;
    
    public StickType GetStickType() => stickType;
    public StickStats GetStats() => currentStats;
    public bool HasStatsGenerated() => statsGenerated;
    
    public void GenerateStats()
    {
        if (statsGenerated) return;
        
        StickTypeRange range = GetRangeForType(stickType);
        
        float randomPower = Mathf.Round(Random.Range(range.powerRange.x, range.powerRange.y));
        float randomSpeed = Mathf.Round(Random.Range(range.speedRange.x, range.speedRange.y));
        
        StickGrade powerGrade = CalculateGrade(randomPower, range.powerRange);
        StickGrade speedGrade = CalculateGrade(randomSpeed, range.speedRange);
        StickGrade overallGrade = CalculateOverallGrade(powerGrade, speedGrade);
        
        currentStats = new StickStats(randomPower, randomSpeed, powerGrade, speedGrade, overallGrade);
        statsGenerated = true;
        
        Debug.Log($"[Stick] Generated stats for {stickType} stick - Power: {randomPower} ({powerGrade}), Speed: {randomSpeed} ({speedGrade}), Overall: {overallGrade}");
    }

    private StickGrade CalculateGrade(float value, Vector2 range)
    {
        float normalizedValue = (value - range.x) / (range.y - range.x);
        
        return normalizedValue switch
        {
            >= 0.8f => StickGrade.S,
            >= 0.6f => StickGrade.A,
            >= 0.4f => StickGrade.B,
            >= 0.2f => StickGrade.C,
            _ => StickGrade.D
        };
    }

    private StickGrade CalculateOverallGrade(StickGrade powerGrade, StickGrade speedGrade)
    {
        float averageGrade = ((float)powerGrade + (float)speedGrade) / 2f;
        int roundedGrade = Mathf.RoundToInt(averageGrade);
        return (StickGrade)Mathf.Clamp(roundedGrade, 0, 4);
    }
    
    private StickTypeRange GetRangeForType(StickType type)
    {
        return type switch
        {
            StickType.Short => shortStickRange,
            StickType.Medium => mediumStickRange,
            StickType.Long => longStickRange,
            _ => mediumStickRange
        };
    }
    
    public void ResetStats()
    {
        statsGenerated = false;
        currentStats = null;
        Debug.Log($"[Stick] Stats reset for {stickType} stick");
    }
    
    [ContextMenu("Generate New Stats")]
    private void ForceGenerateStats()
    {
        statsGenerated = false;
        GenerateStats();
    }
    
    [ContextMenu("Show Current Stats")]
    private void ShowStats()
    {
        if (currentStats != null)
        {
            Debug.Log($"[Stick] {stickType} - Power: {currentStats.power} ({currentStats.powerGrade}), Speed: {currentStats.speed} ({currentStats.speedGrade}), Overall Grade: {currentStats.overallGrade}");
        }
        else
        {
            Debug.Log($"[Stick] {stickType} - No stats generated yet");
        }
    }

    public string GetStatsDescription()
    {
        if (currentStats == null) return "No stats generated";
        
        return $"{stickType} Stick\nPower: {currentStats.power} ({currentStats.powerGrade})\nSpeed: {currentStats.speed} ({currentStats.speedGrade})\nOverall: {currentStats.overallGrade}";
    }
}
