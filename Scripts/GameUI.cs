using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField]
    Image timeBar;

    [SerializeField]
    Image timeBarBg;

    Color panicColor = new (.5f, .1f, .1f, 1f);

    private void FixedUpdate()
    {
        if (Player.Instance.Dead)
        {
            timeBarBg.color = Color.clear;
            return;
        }
        float ratio = LevelManager.Instance.CurrentTimeRatio;
        timeBar.fillAmount = ratio;

        Color bgColor = Color.black;
        if (LevelManager.Instance.CountingDown)
        {
            bgColor = Color.Lerp(Color.black, panicColor, Mathf.Abs(Mathf.Sin(Time.time * 3)) * (-ratio + 1));
        }


        timeBarBg.color = bgColor;
    }

}