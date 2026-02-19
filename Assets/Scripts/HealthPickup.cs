using UnityEngine;
using System.Collections;
using TMPro; // only needed if you want to set "+X" text

[RequireComponent(typeof(Collider2D))]
public class HealthPickup : MonoBehaviour
{
    [Header("Healing")]
    [SerializeField] int healthRestore = 5;
    [SerializeField] string playerTag = "Player";

    [Header("FX")]
    [SerializeField] AudioSource pickupSource; // optional
    [SerializeField] Vector3 spinRotationSpeed = new Vector3(0, 100, 0);

    [Header("Floating Text (optional)")]
    [SerializeField] GameObject healthTextPrefab;   // ← assign your HealthText prefab here
    [SerializeField] Vector3 textOffset = new Vector3(0, 1f, 0);

    [Header("Safety")]
    [SerializeField] float spawnGrace = 0.15f;

    private Collider2D col;
    private bool consumed = false;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (pickupSource == null) pickupSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        if (col != null) col.enabled = false;
        StartCoroutine(EnableAfterGrace());
    }

    IEnumerator EnableAfterGrace()
    {
        yield return new WaitForSeconds(spawnGrace);
        if (col != null) col.enabled = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        if (!other.CompareTag(playerTag)) return;

        var damagable = other.GetComponent<Damagable>();
        if (damagable == null) return;

        if (damagable.Heal(healthRestore))
        {
            consumed = true;

            // Spawn floating text if assigned
            if (healthTextPrefab != null)
            {
                var go = Instantiate(healthTextPrefab, transform.position + textOffset, Quaternion.identity);

                // OPTIONAL: if the prefab has a TMP text, set it to "+X"
                var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = $"+{healthRestore}";
            }

            if (pickupSource && pickupSource.clip != null)
                AudioSource.PlayClipAtPoint(pickupSource.clip, transform.position, pickupSource.volume);

            Destroy(gameObject);
        }
    }

    void Update()
    {
        transform.Rotate(spinRotationSpeed * Time.deltaTime);
    }
}
