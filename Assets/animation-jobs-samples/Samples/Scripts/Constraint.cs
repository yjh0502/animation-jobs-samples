using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class Constraint : MonoBehaviour
{
    public float minX, maxX, minY, maxY, minZ, maxZ;

    public float magnitude;
    public Vector3 forward, up;
    public Quaternion lookRotation, rotation;

    public Vector3 eulerAngles, clampedAngles;

    public void UpdateVecWithParent()
    {
        var parent = transform.parent;
        forward = parent.position - transform.position;
        magnitude = forward.magnitude;

        lookRotation = Quaternion.LookRotation(forward, Vector3.up);
        up = lookRotation * Vector3.up;

        rotation = transform.rotation;
    }

    void OnDrawGizmos()
    {
        var baseRotation = transform.rotation * Quaternion.Inverse(rotation);

        Vector3 up = baseRotation * this.up;
        Vector3 forward = baseRotation * this.forward;
        Vector3 left = Quaternion.AngleAxis(90, up) * forward;

        float size = 1.0f;

        // around Y
        {
            Handles.color = transparent(Color.green);
            Vector3 start = Quaternion.AngleAxis(minY, up) * forward;
            Handles.DrawSolidArc(transform.position, up, start, maxY - minY, size);

            Handles.DrawLine(transform.position, transform.position + up);
        }

        // around X
        {
            Handles.color = transparent(Color.red);
            Vector3 start = Quaternion.AngleAxis(-maxX, left) * forward;
            Handles.DrawSolidArc(transform.position, left, start, maxX - minX, size);
        }

        // around Z
        {
            Handles.color = transparent(Color.blue);
            Vector3 start = Quaternion.AngleAxis(minZ, forward) * up;
            Handles.DrawSolidArc(transform.position, forward, start, maxZ - minZ, size);

            Handles.DrawLine(transform.position, transform.position + forward);
        }

        {
            // var baseRotation = transform.rotation * Quaternion.Inverse(rotation) * lookRotation;
            var baseRotation2 = baseRotation * lookRotation;

            var rot = Quaternion.LookRotation(transform.parent.position - transform.position, Vector3.up);
            var inv = Quaternion.Inverse(rot) * baseRotation2;

            eulerAngles = inv.eulerAngles;

            Handles.color = Color.blue;
            Handles.DrawLine(transform.position, transform.position + inv * Vector3.forward);

            clampedAngles = eulerAngles;
            clampedAngles.x = clampAngle(eulerAngles.x, minX, maxX);
            clampedAngles.y = clampAngle(eulerAngles.y, minY, maxY);
            var clampedInv = Quaternion.Euler(clampedAngles.x, clampedAngles.y, clampedAngles.z);
            var clampedRot = baseRotation2 * Quaternion.Inverse(clampedInv);

            Handles.color = Color.green;
            Handles.DrawLine(transform.position, transform.position + clampedRot * Vector3.forward);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position + (clampedRot * Vector3.forward) * magnitude, 0.05f);
        }
    }

    public Vector3 ClampedPosition(Vector3 parentPosition, Vector3 position)
    {
        var rot = ClampedParentRotation(parentPosition, position);
        return parentPosition - rot * Vector3.forward * magnitude;
    }

    public Vector3 ClampedParentPosition(Vector3 parentPosition, Vector3 position)
    {
        var rot = ClampedParentRotation(parentPosition, position);
        return position + rot * Vector3.forward * magnitude;
    }

    public Quaternion ClampedParentRotation(Vector3 parentPosition, Vector3 position)
    {
        var baseRotation = transform.rotation * Quaternion.Inverse(rotation);
        var baseRotation2 = baseRotation * lookRotation;

        var rot = Quaternion.LookRotation(parentPosition - position, Vector3.up);
        var inv = Quaternion.Inverse(rot) * baseRotation2;

        var eulerAngles = inv.eulerAngles;

        var clampedAngles = eulerAngles;
        // clampedAngles.x = clampAngle(eulerAngles.x, minX, maxX);
        // clampedAngles.y = clampAngle(eulerAngles.y, minY, maxY);
        // clampedAngles.z = 0;
        var clampedInv = Quaternion.Euler(clampedAngles.x, clampedAngles.y, clampedAngles.z);
        var clampedRot = baseRotation2 * Quaternion.Inverse(clampedInv);

        return clampedRot;
    }

    private float clampAngle(float v, float min, float max)
    {
        while (v > 180)
        {
            v -= 360;
        }
        return Mathf.Clamp(v, min, max);
    }

    private Color transparent(Color color)
    {
        color.a = 0.2f;
        return color;
    }
}
