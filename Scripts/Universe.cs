using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.VFX;

public class Universe : MonoBehaviour
{
    [SerializeField]
    int required;
    [SerializeField]
    TextMeshPro text;

    [SerializeField]
    GameObject seed;
    [SerializeField]
    GameObject universe;
    private void Start()
    {
        universe.transform.localScale = Vector3.zero;
        seed.transform.localScale = Vector3.one;

        LevelManager.Instance.LevelComplete += FinishedLevel;
    }
    bool switched = false;
    [SerializeField]
    VisualEffect popEffect;
    IEnumerator SwitchModel()
    {
        yield return new WaitForSeconds(.5f);
        while (seed.transform.localScale.magnitude.AbsoluteDifference(0f) > .01f)
        {
            seed.transform.localScale = Vector3.Lerp(seed.transform.localScale, Vector3.zero, 40f * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }

        yield return this.PlayVFX(popEffect);

        Vector3 target = Vector3.one * 1.5f;
        universe.transform.localScale = Vector3.zero;
        universe.gameObject.SetActive(true);

        while (universe.transform.localScale.magnitude.AbsoluteDifference(target.magnitude) > .01f)
        {
            universe.transform.localScale = Vector3.Lerp(universe.transform.localScale, target, 60f * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
    }
    public void FinishedLevel()
    {
        StartCoroutine(FinishedLevelRoutine());
    }
    readonly int powerProperty = Shader.PropertyToID("_Power");
    IEnumerator FinishedLevelRoutine()
    {
        MeshRenderer renderer = universe.GetComponent<MeshRenderer>();

        float glow = 0f;
        while (universe.transform.localScale.magnitude.AbsoluteDifference(0) > .01f)
        {
            renderer.material.SetFloat(powerProperty, glow);
            universe.transform.localScale = Vector3.Lerp(universe.transform.localScale, Vector3.zero, 20f * Time.fixedDeltaTime);
            
            glow += 10f * Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }
    }
    public bool Enter(int amount)
    {
        if (amount >= required)
        {
            LevelManager.Instance.Complete();
            return true;
        }
        return false;
    }

    private void FixedUpdate()
    {
        text.text = $"{Player.Instance.Collected}/{required}";

        if (!switched && Player.Instance.Collected == required)
        {
            StartCoroutine(SwitchModel());
            switched = true;
        }
    }
}