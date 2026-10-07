using UnityEngine;
using Archaeo.UI;

public class BackgroundTest : MonoBehaviour
{
    [SerializeField] MainWindowView view;
    float t;

    void Update()
    {
        t += Time.unscaledDeltaTime;
        view.SetProgress((t % 60f) / 60f);
        view.SetCoins((long)t);
    }
}