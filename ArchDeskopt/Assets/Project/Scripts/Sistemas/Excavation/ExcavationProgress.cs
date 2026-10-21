using System;

namespace Archaeo.Excavation
{
    /// Lógica pura de la barra de excavación (sin dependencias de Unity).
    /// - Acumula Puntos de Actividad (PA).
    /// - Al llegar al umbral se produce un hallazgo y el sobrante se conserva.
    /// - Cada PA tiene una pequeña probabilidad de provocar un hallazgo anticipado
    ///   (en ese caso la barra se reinicia a 0).
    public class ExcavationProgress
    {
        /// PA necesarios para un hallazgo.
        public float Threshold { get; set; } = 1000f;

        /// Probabilidad, por cada PA, de que salte un hallazgo sin llenar la barra.
        public float EarlyFindChancePerPoint { get; set; } = 0.0003f;

        /// PA acumulados en la barra actual.
        public float Points { get; private set; }

        /// Hallazgos totales (el último valor es el número del hallazgo más reciente).
        public int TotalFinds { get; private set; }

        /// Progreso de 0 a 1 para pintar la barra.
        public float Normalized => Threshold <= 0f ? 0f : Math.Min(1f, Points / Threshold);

        /// (número de hallazgo, ¿fue anticipado por azar?)
        public event Action<int, bool> FindMade;

        /// Se dispara cada vez que cambia el progreso.
        public event Action Changed;

        readonly Random rng = new Random();

        public void AddPoints(float points)
        {
            if (points <= 0f || Threshold <= 0f) return;

            // Hallazgo anticipado: la barra se reinicia
            if (RollEarlyFind(points))
            {
                Points = 0f;
                RaiseFind(true);
                Changed?.Invoke();
                return;
            }

            Points += points;

            // Hallazgo normal: el progreso sobrante pasa a la barra siguiente
            while (Points >= Threshold)
            {
                Points -= Threshold;
                RaiseFind(false);
            }

            Changed?.Invoke();
        }

        public void ResetBar()
        {
            Points = 0f;
            Changed?.Invoke();
        }

        /// Restaura un estado guardado (no dispara hallazgos).
        public void SetState(float points, int totalFinds)
        {
            Points = Math.Max(0f, points);
            TotalFinds = Math.Max(0, totalFinds);
            Changed?.Invoke();
        }

        bool RollEarlyFind(float points)
        {
            float p = EarlyFindChancePerPoint;
            if (p <= 0f) return false;

            // Probabilidad de al menos un acierto en "points" intentos
            double chance = 1.0 - Math.Pow(1.0 - p, points);
            return rng.NextDouble() < chance;
        }

        void RaiseFind(bool early)
        {
            TotalFinds++;
            FindMade?.Invoke(TotalFinds, early);
        }
    }
}