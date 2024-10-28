using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] CanvasGroup _canvasGroup;

    const float FADE_SPEED = 5f;

    public void InstantIn()
    {
        _canvasGroup.alpha = 1;
        gameObject.SetActive(true);
    }

    public void InstantOut()
    {
        _canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    public IEnumerator FadeIn()
    {
        _canvasGroup.alpha = 0;
        _canvasGroup.gameObject.SetActive(true);
        while (_canvasGroup.alpha < 1f)
        {
            _canvasGroup.alpha += FADE_SPEED * Time.deltaTime;
            yield return null;
        }
    }

    public IEnumerator FadeOut()
    {
        _canvasGroup.alpha = 1;
        _canvasGroup.gameObject.SetActive(true);
        while (_canvasGroup.alpha > 0f)
        {
            _canvasGroup.alpha -= FADE_SPEED * Time.deltaTime;
            yield return null;
        }
        _canvasGroup.gameObject.SetActive(false);
    }
}
