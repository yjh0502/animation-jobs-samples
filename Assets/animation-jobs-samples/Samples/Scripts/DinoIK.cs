using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoIK : MonoBehaviour
{
    public Transform target;
    public int length;

    GameObject effector;
    public List<Transform> transforms;
    public List<Quaternion> rotations;

    // Start is called before the first frame update
    void Start()
    {
        effector = SampleUtility.CreateEffector("Effector_" + target.name, target.position, target.rotation);
    }

    public void InitTransforms()
    {
        transforms = new List<Transform>(length);

        var cur = target;
        for (var i = 0; i < length; i++)
        {
            Debug.Log($"adding {cur.gameObject.name}");
            transforms.Add(cur);

            cur = cur.parent;
            if (cur == null)
            {
                break;
            }
        }

        rotations = new List<Quaternion>(length);
        rotations.Add(Quaternion.identity);
        for (var i = 0; i < transforms.Count - 1; i++)
        {
            var child = transforms[i];
            var parent = transforms[i + 1];

            var lookVec = child.position - parent.position;
            var lookAtRot = Quaternion.LookRotation(lookVec, Vector3.up);
            var rotation = parent.rotation;

            var lookToRot = Quaternion.Inverse(lookAtRot) * rotation;
            rotations.Add(lookToRot);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var dist = effector.transform.position - transforms[0].position;
        if (dist.magnitude < 0.01f)
        {
            return;
        }

        Step();
    }

    void Step()
    {
        // posList[0] == head
        var posList = new Vector3[transforms.Count];
        for (var i = 0; i < transforms.Count; i++)
        {
            posList[i] = transforms[i].position;
        }

        var startPos = effector.transform.position;
        var endPos = posList[posList.Length - 1];

        // forward path, target to root
        var targetPos = startPos;
        for (var i = 0; i < transforms.Count - 1; i++)
        {
            var childIdx = i;
            var parentIdx = i + 1;
            var childConstraint = transforms[childIdx].GetComponent<Constraint>();
            Vector3 nextTail;
            if (childConstraint == null)
            {
                forwardBackwardPass(posList[childIdx], posList[parentIdx], targetPos, out nextTail);
            }
            else
            {
                nextTail = childConstraint.ClampedParentPosition(posList[parentIdx], targetPos);
            }

            posList[childIdx] = targetPos;
            targetPos = nextTail;
        }
        posList[transforms.Count - 1] = targetPos;

        // backward path, root to target
        targetPos = endPos;
        for (var i = transforms.Count - 1; i > 0; i--)
        {
            var childIdx = i - 1;
            var parentIdx = i;
            var childConstraint = transforms[childIdx].GetComponent<Constraint>();
            Vector3 nextTail;
            if (childConstraint == null)
            {
                forwardBackwardPass(posList[parentIdx], posList[childIdx], targetPos, out nextTail);
            }
            else
            {
                nextTail = childConstraint.ClampedPosition(targetPos, posList[childIdx]);
            }

            posList[parentIdx] = targetPos;
            targetPos = nextTail;
        }
        posList[0] = targetPos;

        for (var i = transforms.Count - 1; i > 0; i--)
        {
            var parent = posList[i];
            var child = posList[i - 1];

            var lookVec = child - parent;
            var lookAtRot = Quaternion.LookRotation(lookVec, Vector3.up);

            transforms[i].rotation = lookAtRot * rotations[i];
        }

        for (var i = transforms.Count - 1; i >= 0; i--)
        {
            transforms[i].position = posList[i];
        }
    }

    void OnDrawGizmos()
    {
        for (var i = 0; i < transforms.Count; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transforms[i].position, 0.1f);
        }

        for (var i = 0; i < transforms.Count - 1; i++)
        {
            Gizmos.DrawLine(transforms[i].position, transforms[i + 1].position);

            var lookVec = transforms[i + 1].position - transforms[i].position;
            var lookAtRot = Quaternion.LookRotation(lookVec, Vector3.up);
        }
    }

    void forwardBackwardPass(Vector3 head, Vector3 tail, Vector3 target, out Vector3 nextTail)
    {
        var delta = target - tail;

        var cdist = (head - tail).magnitude;
        var sdist = delta.magnitude;

        var scale = cdist / sdist;

        nextTail = target - delta * scale;
    }
}
