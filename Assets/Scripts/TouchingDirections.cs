using UnityEngine;

public class TouchingDirections : MonoBehaviour
{
    public ContactFilter2D castFilter;
    public float groundDistance = 0.05f;
    public float wallDistance = 0.2f;
    public float ceilingDistance = 0.05f;
    CapsuleCollider2D touchingCol;
    Animator animator;
    RaycastHit2D[] groundHits = new RaycastHit2D[5];
    RaycastHit2D[] wallHits = new RaycastHit2D[5];
    RaycastHit2D[] CeilingHits = new RaycastHit2D[5];

    [SerializeField]
    private bool _isGrounded;
    public bool IsGrounded {
        get
        {
            return _isGrounded;
        } private set
        {
            _isGrounded = value;
            animator.SetBool(AnimationStrings.isGrounded, value);
    } }

    [SerializeField]
    private bool _isOnWall;
    public bool IsOnWall {
        get
        {
            return _isOnWall;
        } private set
        {
            _isOnWall= value;
            animator.SetBool(AnimationStrings.isOnWall, value);
    } }

 [SerializeField]
    private bool _isOnCeiling;
    private Vector2 wallCheckDirection => gameObject.transform.localScale.x > 0 ? Vector2.right : Vector2.left;

    public bool IsOnCeiling {
        get
        {
            return _isOnCeiling;
        } private set
        {
            _isOnCeiling= value;
            animator.SetBool(AnimationStrings.isOnCeiling, value);
    } }

private bool IsGroundedCheck()
{
    Bounds bounds = touchingCol.bounds;
    float extraHeight = 0.15f;

    Vector2 center = new Vector2(bounds.center.x, bounds.min.y); // Cast from bottom

    RaycastHit2D centerHit = Physics2D.Raycast(center, Vector2.down, extraHeight, castFilter.layerMask);
    RaycastHit2D leftHit = Physics2D.Raycast(center + Vector2.left * bounds.extents.x * 0.9f, Vector2.down, extraHeight, castFilter.layerMask);
    RaycastHit2D rightHit = Physics2D.Raycast(center + Vector2.right * bounds.extents.x * 0.9f, Vector2.down, extraHeight, castFilter.layerMask);

    Debug.DrawRay(center, Vector2.down * extraHeight, Color.red);
    Debug.DrawRay(center + Vector2.left * bounds.extents.x * 0.9f, Vector2.down * extraHeight, Color.green);
    Debug.DrawRay(center + Vector2.right * bounds.extents.x * 0.9f, Vector2.down * extraHeight, Color.blue);

    return centerHit.collider != null || leftHit.collider != null || rightHit.collider != null;
}

    private void Awake()
    {
        touchingCol = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();
    }





    void FixedUpdate()
{
    IsGrounded = IsGroundedCheck(); // ← Better slope detection
    IsOnWall = touchingCol.Cast(wallCheckDirection, castFilter, wallHits, wallDistance) > 0;
    IsOnCeiling = touchingCol.Cast(Vector2.up, castFilter, CeilingHits, ceilingDistance) > 0;
}
}

