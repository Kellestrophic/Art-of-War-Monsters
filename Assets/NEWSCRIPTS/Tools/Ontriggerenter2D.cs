using UnityEngine;

public class TriggerDebug : MonoBehaviour
{
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("MELEE TRIGGER HIT: " + other.name + " layer=" + LayerMask.LayerToName(other.gameObject.layer));
    }
}
