
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public static class ScuffedUtils
{

    public static float AbsoluteDifference(this float f, float to)
    {
        return Mathf.Abs(f - to);
    }
    public static float MagnitudeDifference(this Vector3 f, Vector3 to)
    {
        return Mathf.Abs(f.magnitude - to.magnitude);
    }
    public static float DotTo(this Transform transform, Vector3 to)
    {
        Vector3 dir = (transform.position - to).normalized;
        return Vector3.Dot(transform.forward, dir);
    }
    public static float DotDir(this Transform transform, Vector3 dir)
    {
        return Vector3.Dot(transform.forward, dir);
    }
    public static Coroutine PlayVFX(this MonoBehaviour behaviour,VisualEffect effect)
    {
        if (effect == null)
            return null;

       return behaviour.StartCoroutine(VFX(effect));
    }
    static IEnumerator VFX(VisualEffect effect)
    {
        effect.Stop();
        effect.Reinit();
        yield return null;
        effect.Play();
    }
}