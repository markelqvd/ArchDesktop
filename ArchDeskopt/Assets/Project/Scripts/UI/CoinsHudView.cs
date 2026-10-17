using System;
using TMPro;
using UnityEngine;
using Archaeo.Economy;

namespace Archaeo.UI
{
    /// Muestra las monedas y los ingresos por segundo.
    /// Colócalo en la raíz de UI_MainWindow.
    public class CoinsHudView : MonoBehaviour
    {
        [SerializeField] TMP_Text coinsText;
        [SerializeField] TMP_Text incomeText;   // opcional: "+1 / s"

        EconomyManager economy;

        void Start()
        {
            economy = EconomyManager.Instance;
            if (economy == null) return;   // p.ej. al probar Main sin pasar por Boot

            economy.CoinsChanged += RefreshCoins;
            economy.IncomeChanged += RefreshIncome;
            RefreshCoins();
            RefreshIncome();
        }

        void OnDestroy()
        {
            if (economy == null) return;
            economy.CoinsChanged -= RefreshCoins;
            economy.IncomeChanged -= RefreshIncome;
        }

        void RefreshCoins()
        {
            coinsText.text = Math.Floor(economy.Coins).ToString("N0");
        }

        void RefreshIncome()
        {
            if (incomeText == null) return;
            incomeText.text = $"+{economy.IncomePerSecond:0.##} / s";
        }
    }
}