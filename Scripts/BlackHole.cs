using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class BlackHole : MonoBehaviour
{
    CollectibleState state = CollectibleState.None;

    public CollectibleState State => state;

    public static float decayRate = 2f;

    [SerializeField]
    MeshRenderer seed;
    [SerializeField]
    MeshRenderer blackHole;
    [SerializeField]
    MeshRenderer plant;
    [SerializeField]
    MeshRenderer dead;

    private void Start()
    {
        LevelManager.StageChange += SeasonChange;
        LevelManager.Instance.CountBlackHole();
        SetModel();
    }

    Coroutine decayRoutine;
    private void SeasonChange()
    {
        if (state != CollectibleState.None)
            return;

        state = CollectibleState.Alive;
        Die();
        //decayRoutine = StartCoroutine(DecayRoutine());
    }
    void Die()
    {
        state = CollectibleState.Dead;
        SetModel();
    }
    //float life;
    //IEnumerator DecayRoutine()
    //{
    //    life = 1f;

    //    SetModel();
    //    while (true)
    //    {
    //        if (state != CollectibleState.None)
    //            yield break;
    //        if (life == 0f)
    //        {
    //            state = CollectibleState.Dead;
    //            SetModel();
    //            yield break;
    //        }
    //        life = LevelManager.Instance.CurrentTimeRatio;

    //        blackHole.transform.localScale = Mathf.Clamp(life * 1.25f, .2f, float.MaxValue) * Vector3.one;

    //        yield return new WaitForFixedUpdate();
    //    }
    //}
    public bool Collect()
    {
        if(decayRoutine != null && state != CollectibleState.Dead)
        {
            StopCoroutine(decayRoutine);
        }
        if (state == CollectibleState.None)
        {
            LevelManager.Instance.BlackHoleTaken();
            //transform.localScale = Vector3.one * 2f;
            state = CollectibleState.Planted;
            SetModel();
            return false;
        }

        if (state == CollectibleState.Alive || state == CollectibleState.Planted)
        {
            state = CollectibleState.Harvested;
            Debug.Log("Collected");
            SetModel();
            return true;
        }
        return false;
    }
    void SetModel()
    {
        if (expand != null)
            StopCoroutine(expand);

        expand = StartCoroutine(ExpandModel());
    }
    [SerializeField]
    VisualEffect popEffect;
    MeshRenderer currentModel;

    Coroutine expand;
    IEnumerator ExpandModel()
    {
        MeshRenderer targetModel = state switch
        {
            CollectibleState.None => seed,
            CollectibleState.Alive => blackHole,
            CollectibleState.Planted => blackHole,
            CollectibleState.Harvested => plant,
            CollectibleState.Dead => dead,
            _ => blackHole
        };
        Vector3 targetScale = Vector3.one;
        if (currentModel)
        {
            while (currentModel.transform.localScale.magnitude.AbsoluteDifference(0f) > .01f)
            {
                currentModel.transform.localScale = Vector3.Lerp(currentModel.transform.localScale, Vector3.zero, 30f * Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }

            yield return this.PlayVFX(popEffect);
            if (currentModel != targetModel)
                currentModel.gameObject.SetActive(false);
        }
        yield return null;
        currentModel = targetModel;

        switch (state)
        {
            case CollectibleState.Alive:
                targetScale *= 1.25f;
                currentModel.material.SetFloat(powerProperty, 1f);
                break;
            case CollectibleState.Planted:
                targetScale *= 1.25f;
                currentModel.material.SetFloat(powerProperty, .2f);
                break;
        }

        yield return this.PlayVFX(popEffect);

        currentModel.transform.localScale = Vector3.zero;
        currentModel.gameObject.SetActive(true);

        while (currentModel.transform.localScale.magnitude.AbsoluteDifference(targetScale.magnitude) > .01f)
        {
            currentModel.transform.localScale = Vector3.Lerp(currentModel.transform.localScale, targetScale, 60f * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
    }
    readonly int powerProperty = Shader.PropertyToID("_Power");
    private void FixedUpdate()
    {
        if (currentModel == blackHole)
            currentModel.material.SetFloat(powerProperty, LevelManager.Instance.CurrentTimeRatio);
    }
}
public enum CollectibleState
{
    None,
    Planted,
    Alive,
    Harvested,
    Dead
}