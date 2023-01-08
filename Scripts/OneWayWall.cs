using System.Collections;
using UnityEngine;

public class OneWayWall : MonoBehaviour
{
    new BoxCollider collider;
    private void Start()
    {
        collider = GetComponent<BoxCollider>();
    }
    public void TryPass(Vector3 dir)
    {
        Debug.Log($"{dir}");
        if (Player.Instance.Bounds.Intersects(collider.bounds))
            return;

        collider.isTrigger = (Vector3.Dot(transform.up, dir) > .5f);
    }
}