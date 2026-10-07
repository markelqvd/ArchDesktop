using System.Collections;
using TMPro;
using UnityEngine;

namespace Archaeo.UI
{
    /// Toast discreto: aparece, espera y se destruye solo.
    [RequireComponent(typeof(CanvasGroup))]
    public class NotificationToast : MonoBehaviour
    {
        [SerializeField] TMP_Text messageText;
        [SerializeField] float visibleSeconds = 3f;
        [SerializeField] float fadeSeconds = 0.4f;

        CanvasGroup group;

        public void Show(string message)
        {
            group = GetComponent<CanvasGroup>();
            messageText.text = message;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(visibleSeconds);
            for (float t = 0; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / fadeSeconds;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}