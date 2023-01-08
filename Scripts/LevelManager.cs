using System.Collections;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    [SerializeField]
    float maxTime;
    float time;

    public event Action LevelComplete;
    public static event Action StageChange;
    public float CurrentTimeRatio => time / maxTime;

    public bool CountingDown { get; private set; }
    [SerializeField]
    [TextArea]
    string endMessage;

    const string failMessage = "Collect everything before decay. It is te only way";
    private void OnEnable()
    {
        Instance = this;
        ResetStatics();
        CountingDown = false;
    }
    int blackHoles = 0;
    public void CountBlackHole()
    {
        blackHoles++;
    }
    int taken;
    public void BlackHoleTaken()
    {
        taken++;

        if (taken == blackHoles && !CountingDown)
        {
            time = maxTime;
        }
    }
    private void Start()
    {
        StartCoroutine(TimeRoutine());
    }

    IEnumerator TimeRoutine()
    {
        while (time != maxTime)
        {
            time += Time.fixedDeltaTime * Player.TimeFlow;
            time = Mathf.Clamp(time, 0, maxTime);
            yield return new WaitForFixedUpdate();
        }
        StageChange?.Invoke();
        CountingDown = true;
        while (time != 0)
        {
            time -= Time.fixedDeltaTime * Player.TimeFlow;
            time = Mathf.Clamp(time, 0, maxTime);
            yield return new WaitForFixedUpdate();
        }

        GameOver();
    }
    bool complete;
    public bool IsComplete => complete;
    public void Complete()
    {
        StopAllCoroutines();

        GameManager.Instance.FinishedLevel(endMessage);
        LevelComplete?.Invoke();
    }
    public void GameOver()
    {
        Player.Instance.Break();
        ResetStatics();
        StartCoroutine(GameOverRoutine());
    }
    IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(2f);
        GameManager.Instance.RestartLevel();
    }
    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }
    void ResetStatics()
    {
        StageChange = null;
    }
}