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

        /// Tiempo mínimo entre dos hallazgos (s). 0 = sin límite.
        /// Si la barra se llena antes, se queda llena hasta que pase el tiempo.
        public float MinSecondsBetweenFinds { get; set; }

        /// Reloj en segundos. Si es null no se aplica el tiempo mínimo.
        public Func<double> Clock { get; set; }

        double lastFindTime = double.NegativeInfinity;

        /// Mientras un QTE está en curso no se lanzan más hallazgos.
        public bool HeldByQte { get; set; }

        /// Empieza a contar el tiempo mínimo desde ahora (al terminar un QTE).
        public void StartCooldown()
        {
            if (Clock != null) lastFindTime = Clock();
        }

        /// PA acumulados en la barra actual.
        public float Points { get; private set; }

        /// Hallazgos totales (el último valor es el número del hallazgo más reciente).
        public int TotalFinds { get; private set; }

        /// La barra está llena pero espera a que pase el tiempo mínimo.
        public bool Waiting => Threshold > 0f && Points >= Threshold && !Ready;

        /// Segundos que faltan para poder lanzar el siguiente hallazgo (0 = ya se puede).
        public float SecondsUntilReady
        {
            get
            {
                if (CooldownOver) return 0f;
                return (float)Math.Max(0.0, MinSecondsBetweenFinds - (Clock() - lastFindTime));
            }
        }

        bool CooldownOver => Clock == null || MinSecondsBetweenFinds <= 0f || Clock() - lastFindTime >= MinSecondsBetweenFinds;
        bool Ready => !HeldByQte && CooldownOver;

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
            if (Ready && RollEarlyFind(points))
            {
                Points = 0f;
                MarkFind();
                RaiseFind(true);
                Changed?.Invoke();
                return;
            }

            Points += points;
            TryFire();
            Changed?.Invoke();
        }

        /// Llamar cada frame: lanza el hallazgo pendiente cuando pasa el tiempo mínimo.
        public void Tick()
        {
            if (Threshold <= 0f || Points < Threshold || !Ready) return;
            TryFire();
            Changed?.Invoke();
        }

        void TryFire()
        {
            // Hallazgo normal: el progreso sobrante pasa a la barra siguiente
            while (Points >= Threshold && Ready)
            {
                Points -= Threshold;
                MarkFind();
                RaiseFind(false);
            }

            // Llena y esperando: no acumular de más
            if (Points > Threshold) Points = Threshold;
        }

        void MarkFind()
        {
            if (Clock != null) lastFindTime = Clock();
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