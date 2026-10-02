using UnityEngine;

// Only carrier-launched swarms use the launch arc; ordinary wave swarms keep their direct flight.
public sealed class CarrierSwarmFlight : MonoBehaviour
{
    Transform carrier;
    Vector2 anchor, anchorOffset;
    Vector2 start, outward, outerCorner, flank, inwardCorner, inward, exit;
    Vector2 approachLane;
    bool reachedLane;
    float elapsed, duration, angularVelocity, launchDelay;

    public void Initialize(Transform source, Bounds hull, float side, int index, int group)
    {
        carrier = source;
        anchor = hull.center;
        anchorOffset = anchor - (Vector2)source.position;
        start = (Vector2)transform.position - anchor;
        float direction = (index + group) % 2 == 0 ? -1f : 1f;
        int arcSlot = index % 7;
        float width = hull.extents.x + 0.4f + arcSlot * 0.16f + Random.Range(-0.08f, 0.08f);
        float height = hull.extents.y + 0.25f + arcSlot * 0.1f + Random.Range(-0.06f, 0.06f);
        outward = new Vector2(direction * Random.Range(0.28f, 0.42f), side * height);
        outerCorner = new Vector2(direction * width, side * height);
        flank = new Vector2(direction * width, 0f);
        inwardCorner = new Vector2(direction * width, -side * height);
        float laneWidth = 0.6f + (index / 2 % 4) * 0.22f + Random.Range(-0.07f, 0.07f);
        inward = new Vector2(direction * laneWidth, -side * (height - 0.2f));
        exit = new Vector2(direction * laneWidth, -side * (height + 0.25f));
        approachLane = new Vector2(direction * laneWidth, side * (1.1f + index % 3 * 0.12f));
        duration = 3.2f + arcSlot * 0.25f + Random.Range(-0.12f, 0.12f);
        launchDelay = Random.Range(0f, 0.22f);
        Vector2 heading = outward - start;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg - 90f);
    }

    public void Tick(Vector3 coreTarget, float speed)
    {
        if (launchDelay > 0f)
        {
            launchDelay -= Time.deltaTime;
            if (carrier) anchor = (Vector2)carrier.position + anchorOffset;
            transform.position = anchor + start;
            return;
        }
        Vector3 before = transform.position;
        if (elapsed < duration)
        {
            if (carrier) anchor = (Vector2)carrier.position + anchorOffset;
            elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
            float progress = elapsed / duration;
            Vector2 offset = progress < 0.5f
                ? Bezier(start, outward, outerCorner, flank, progress * 2f)
                : Bezier(flank, inwardCorner, inward, exit, (progress - 0.5f) * 2f);
            transform.position = anchor + offset;
        }
        else
        {
            Vector3 laneTarget = coreTarget + (Vector3)approachLane;
            if (Vector3.Distance(transform.position, laneTarget) < 0.1f) reachedLane = true;
            transform.position = Vector3.MoveTowards(transform.position, reachedLane ? coreTarget : laneTarget, speed * Time.deltaTime);
        }

        Vector2 direction = transform.position - before;
        if (direction.sqrMagnitude < 0.000001f) return;
        float desiredAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.z, desiredAngle, ref angularVelocity,
            0.15f, 540f, Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        float remaining = 1f - t;
        return remaining * remaining * remaining * a + 3f * remaining * remaining * t * b +
            3f * remaining * t * t * c + t * t * t * d;
    }
}
