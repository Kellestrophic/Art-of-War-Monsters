using UnityEngine;

[DisallowMultipleComponent]
public class StaticCamera : MonoBehaviour
{
    public bool lockPosition = true;
    public bool lockRotation = true;

    private Vector3 _pos;
    private Quaternion _rot;

    private void Awake()
    {
        _pos = transform.position;
        _rot = transform.rotation;
    }

    private void LateUpdate()
    {
        if (lockPosition) transform.position = _pos;
        if (lockRotation) transform.rotation = _rot;
    }
}
