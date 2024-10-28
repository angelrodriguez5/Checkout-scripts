using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CanvasGroupExtensions
{
    public static IEnumerator FadeOut(this CanvasGroup canvas, float seconds = 0.2f)
    {
        canvas.gameObject.SetActive(true);
        canvas.alpha = 1;

        yield return null;
        yield return null;
        while (canvas.alpha > 0)
        {
            canvas.alpha -= Time.deltaTime / seconds;
            yield return null;
        }

        canvas.alpha = 0;
        canvas.gameObject.SetActive(false);
    }

    public static IEnumerator FadeIn(this CanvasGroup canvas, float seconds = 0.2f)
    {
        canvas.gameObject.SetActive(true);
        canvas.alpha = 0;

        yield return null;
        yield return null;
        while (canvas.alpha < 1)
        {
            canvas.alpha += Time.deltaTime / seconds;
            yield return null;
        }

        canvas.alpha = 1;
    }
}
