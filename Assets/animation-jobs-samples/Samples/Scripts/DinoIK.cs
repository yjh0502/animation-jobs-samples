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
        // var dist = effector.transform.position - transforms[0].position;
        // if (dist.magnitude < 0.01f)
        // {
        //     return;
        // }

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

        // forward path
        var targetPos = startPos;
        for (var i = 0; i < transforms.Count - 1; i++)
        {
            var constraint = transforms[i].GetComponent<Constraint>();
            Vector3 nextTail;
            if (constraint == null)
            {
                forwardBackwardPass(posList[i], posList[i + 1], targetPos, out nextTail);
            }
            else
            {
                nextTail = constraint.ClampedParentPosition(posList[i + 1], targetPos);
            }

            posList[i] = targetPos;
            targetPos = nextTail;
        }
        posList[transforms.Count - 1] = targetPos;

        // backward path
        targetPos = endPos;
        for (var i = transforms.Count - 1; i > 0; i--)
        {
            var constraint = transforms[i - 1].GetComponent<Constraint>();
            Vector3 nextTail;
            if (constraint == null)
            {
                forwardBackwardPass(posList[i], posList[i - 1], targetPos, out nextTail);
            }
            else
            {
                nextTail = constraint.ClampedPosition(targetPos, posList[i - 1]);
            }

            posList[i] = targetPos;
            targetPos = nextTail;
        }
        posList[0] = targetPos;

        for (var i = transforms.Count - 1; i >= 0; i--)
        {
            transforms[i].position = posList[i];
        }

        for (var i = transforms.Count - 1; i > 0; i--)
        {
            var parent = transforms[i];
            var child = transforms[i - 1];

            var lookVec = child.position - parent.position;
            var lookAtRot = Quaternion.LookRotation(lookVec, Vector3.up);

            parent.rotation = lookAtRot * rotations[i];
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
