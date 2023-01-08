using System.Collections;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    static GameManager _instance;
    public static GameManager Instance => _instance;
    [SerializeField]
    Image fadeImage;

    [SerializeField]
    TextMeshProUGUI text;

    [SerializeField]
    AudioSource musicSource;

    const int scenesEnd = 11;

    float originalVol;
    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            return;
        }

        originalVol = musicSource.volume;
        musicSource.volume = 0;

        _instance = this;
    }
    private void Start()
    {
        _instance = this;
        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(this);

        StartCoroutine(IntroRoutine());
    }

    IEnumerator IntroRoutine()
    {
        try
        {
            if (SceneManager.GetActiveScene().buildIndex != 0)
            {
                musicSource.volume = 1f;
                yield break;
            }
        }
        catch
        {
            musicSource.volume = 1f;
            musicSource.Play();
            yield break;
        }
        yield return new WaitForSeconds(3f);

        levelComplete = true;
        FadeImage(true);
        yield return new WaitUntil(() => fadeDone);
        musicSource.Play();
        LoadScene(1);
        yield return null;
    }

    string currentMessage = "Your time has come. You will gather what we need. Reach infinity...";
    bool loadingLevel = false;
    bool levelComplete;
    int lastLoadedScene;
    public void FinishedLevel(string message)
    {
        if (loadingLevel)
            return;

        levelComplete = true;
        loadingLevel = true;
        currentMessage = $"Dimension {lastLoadedScene}:\n\n{message}";
        Player.Instance.Break(true);
        StartCoroutine(FinishedLevelRoutine());
    }
    bool final;
    IEnumerator FinishedLevelRoutine()
    {
        FadeImage(true);
        yield return new WaitUntil(() => fadeDone);
        int buildIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (buildIndex >= scenesEnd)
        {
            buildIndex = 0;
        }
        Scene scene = SceneManager.GetSceneByBuildIndex(buildIndex);
        LoadScene(buildIndex);
        yield return null;
    }

    public void RestartLevel()
    {
        if (loadingLevel)
            return;
        loadingLevel = true;
        StartCoroutine(RestartLevelRoutine());
    }
    IEnumerator RestartLevelRoutine()
    {
        FadeImage(true);
        yield return new WaitUntil(() => fadeDone);
        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        Scene scene = SceneManager.GetSceneByBuildIndex(buildIndex);
        LoadScene(buildIndex);
        yield return null;
    }
    void LoadScene(int buildIndex)
    {
        lastLoadedScene = buildIndex;
        AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex);
        op.completed += (e) =>
        {
            levelComplete = false;
            FadeImage(false);
            StartLevel();
            loadingLevel = false;

            if(buildIndex == scenesEnd)
            {
                final = true;

                StartCoroutine(EndRoutine());
            };
        };
    }
    const string endMessage = "Thank you for playing.";
    IEnumerator EndRoutine()
    {
        yield return new WaitForSeconds(3f);
        levelComplete = true;
        Player.Instance.Break();
        FinishedLevel(endMessage);
    }
    //Still dont know what to put here :|
    void StartLevel()
    {

    }
    void FadeImage(bool to)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        text.text = currentMessage;
        currentFade = StartCoroutine(FadeImageRoutine(to));
    }
    Coroutine currentFade;
    bool fadeDone;
    IEnumerator FadeImageRoutine(bool to)
    {
        fadeDone = false;
        float start = to ? 0f : 1.5f;
        float end = to ? 1f : -.5f;
        float cur = start;
        while (cur.AbsoluteDifference(end) > .05f)
        {
            cur = Mathf.Clamp01(Mathf.Lerp(cur, end, (to ? 8f : 15f) * Time.fixedDeltaTime));

            musicSource.volume = (-cur + 1) * originalVol;
            fadeImage.fillAmount = cur;
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForSeconds(.2f);

        fadeImage.fillAmount = Mathf.Clamp01(end);
        musicSource.volume = (-fadeImage.fillAmount + 1) * originalVol;

        if (levelComplete)
        {
            float prog = 0f;
            while(text.color != Color.white)
            {
                text.color = Color.Lerp(text.color, Color.white, prog);
                prog += .5f * Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            yield return new WaitForSeconds(1f);
            //Extend by charactes in message
            for (int i = 0; i < currentMessage.Length; i++)
            {
                yield return new WaitForSeconds(0.025f);
            }
            prog = 0f;
            while (text.color != Color.clear)
            {
                text.color = Color.Lerp(text.color, Color.clear, prog);
                prog += .5f * Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            if (currentMessage == endMessage)
                Application.Quit();
        }
        fadeDone = true;
    }
}