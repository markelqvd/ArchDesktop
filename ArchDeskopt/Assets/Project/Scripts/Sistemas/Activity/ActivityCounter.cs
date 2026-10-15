using System;

namespace Archaeo.Activity
{
    /// Lleva la cuenta de la actividad aceptada y la convierte en Puntos de Actividad (PA).
    /// Solo guarda contadores numéricos.
    public class ActivityCounter
    {
        public long MouseEvents { get; private set; }
        public long KeyboardEvents { get; private set; }
        public long RejectedEvents { get; private set; }   // descartados por el límite por segundo
        public long TotalEvents => MouseEvents + KeyboardEvents;
        public double TotalPoints { get; private set; }

        /// PA que genera cada evento (GDD: 1 inicialmente; la mejora "Potencia de excavación" lo subirá).
        public float PointsPerEvent { get; set; } = 1f;

        /// Se dispara con cada evento aceptado, con los puntos que ha dado.
        public event Action<float> PointsAdded;

        public void Register(ActivityEvent e)
        {
            if (e.Kind == ActivityKind.Mouse) MouseEvents++;
            else KeyboardEvents++;

            TotalPoints += PointsPerEvent;
            PointsAdded?.Invoke(PointsPerEvent);
        }

        public void RegisterRejected() => RejectedEvents++;

        public void Reset()
        {
            MouseEvents = KeyboardEvents = RejectedEvents = 0;
            TotalPoints = 0;
        }
    }
}