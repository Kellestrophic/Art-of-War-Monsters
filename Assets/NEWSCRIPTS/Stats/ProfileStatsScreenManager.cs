using UnityEngine;
using TMPro;

public class ProfileStatsScreenManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentRoot;          // Content inside ScrollView
    [SerializeField] private GameObject statRowPrefab;       // EnemyStatRow prefab

    private EnemyBossLibrary lib;

    private void Awake()
    {
        lib = Resources.Load<EnemyBossLibrary>("EnemyBossLibrary");
        if (lib == null)
        {
            Debug.LogError("[ProfileStatsScreenManager] Missing EnemyBossLibrary in Resources!");
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

    // ----------------------------------------------------------
    // MAIN REFRESH
    // ----------------------------------------------------------
   public void Refresh()
{
    if (StatsTracker.Instance == null)
    {
        Debug.LogError("[StatsScreen] StatsTracker missing!");
        return;
    }

    var store = RuntimeStatsStore.Instance;
    var profile = store?.GetProfile();

    if (profile == null)
    {
        Debug.LogWarning("[StatsScreen] No active profile!");
        return;
    }

    // Clear old content
    foreach (Transform child in contentRoot)
        Destroy(child.gameObject);

    // ----------------------
    //  ENEMIES SECTION
    // ----------------------
    AddRow("— ENEMIES —", -1, isHeader: true);

    foreach (string enemyKey in lib.AllEnemyKeys())
    {
        int kills = 0;

        if (profile.enemyKills != null)
            profile.enemyKills.TryGetValue(enemyKey, out kills);

        SpawnEnemyRow(enemyKey, kills);
    }

    // ----------------------
    //  BOSSES SECTION
    // ----------------------
    AddRow("— BOSSES —", -1, isHeader: true);

    foreach (string bossKey in lib.AllBossKeys())
    {
        int kills = 0;

        if (profile.bossKills != null)
            profile.bossKills.TryGetValue(bossKey, out kills);

        SpawnBossRow(bossKey, kills);
    }
}

    // ----------------------------------------------------------
    //  ROW SPAWN HELPERS
    // ----------------------------------------------------------
    private void SpawnEnemyRow(string prettyName, int count)
    {
        AddRow(prettyName, count);
    }

    private void SpawnBossRow(string prettyName, int count)
    {
        AddRow(prettyName, count);
    }

    // ----------------------------------------------------------
    //  UNIVERSAL ROW BUILDER
    // ----------------------------------------------------------
    private void AddRow(string name, int value, bool isHeader = false)
    {
        GameObject row = Instantiate(statRowPrefab, contentRoot);

        TMP_Text nameText = row.transform.Find("EnemyNameText").GetComponent<TMP_Text>();
        TMP_Text valueText = row.transform.Find("EnemyValueText").GetComponent<TMP_Text>();

        nameText.text = name;

        if (isHeader)
        {
            valueText.text = "";
            nameText.fontStyle = FontStyles.Bold;
            return;
        }

        valueText.text = value.ToString();
    }
}
