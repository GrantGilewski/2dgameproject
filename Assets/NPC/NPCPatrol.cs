using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class NPCPatrol : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 1.4f;               // How fast the NPC moves
    public float arriveThreshold = 0.05f;    // When we consider a waypoint reached
    public float waitAtPointSeconds = 1.0f;  // Small pause at each waypoint
    public bool loop = true;

    [Header("Path")]
    public Transform[] waypoints;            // Drag empties here in order

    Rigidbody2D rb;
    Animator anim;

    // We cache the last non-zero direction so the NPC keeps facing that way while idle
    Vector2 lastMoveDir = Vector2.down; // default: front
    int index = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (waypoints != null && waypoints.Length > 0)
            StartCoroutine(PatrolRoutine());
        else
            SetAnim(Vector2.zero);
    }

    IEnumerator PatrolRoutine()
    {
        while (true)
        {
            if (waypoints == null || waypoints.Length == 0) yield break;

            Vector2 target = waypoints[index].position;
            // Move until close enough
            while (Vector2.Distance(transform.position, target) > arriveThreshold)
            {
                Vector2 dir = (target - (Vector2)transform.position).normalized;

                // Move using Rigidbody2D so collisions/triggers behave properly
                rb.MovePosition((Vector2)transform.position + dir * speed * Time.fixedDeltaTime);

                // Update Animator facing from motion
                SetAnim(dir);

                yield return new WaitForFixedUpdate();
            }

            // Arrived at waypoint: stop and idle
            rb.linearVelocity = Vector2.zero;
            SetAnim(Vector2.zero);

            // Small wait
            yield return new WaitForSeconds(waitAtPointSeconds);

            // Next waypoint
            index++;
            if (index >= waypoints.Length)
            {
                if (loop) index = 0;
                else yield break;
            }
        }
    }

    /// <summary>
    /// Updates Animator parameters based on movement vector.
    /// Non-zero: update facing and mark IsMoving=true.
    /// Zero: keep last facing and mark IsMoving=false.
    /// </summary>
    void SetAnim(Vector2 move)
    {
        if (move.sqrMagnitude > 0.0001f)
        {
            // Optionally “snap” to the dominant axis to avoid diagonal blends:
            // move = SnapToCardinal(move);

            lastMoveDir = move.normalized;
            anim.SetBool("IsMoving", true);
            anim.SetFloat("MoveX", lastMoveDir.x);
            anim.SetFloat("MoveY", lastMoveDir.y);
        }
        else
        {
            anim.SetBool("IsMoving", false);
            anim.SetFloat("MoveX", lastMoveDir.x);
            anim.SetFloat("MoveY", lastMoveDir.y);
        }
    }

    // Use this if your art uses pure 4-direction facings (no diagonals)
    Vector2 SnapToCardinal(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return new Vector2(Mathf.Sign(v.x), 0f);
        else
            return new Vector2(0f, Mathf.Sign(v.y));
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (!waypoints[i]) continue;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.07f);
            if (i + 1 < waypoints.Length && waypoints[i + 1])
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
        if (loop && waypoints[0] && waypoints[^1])
            Gizmos.DrawLine(waypoints[^1].position, waypoints[0].position);
    }
#endif
}
