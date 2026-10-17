namespace Archaeo.Activity
{
    /// Limita cuántos eventos se aceptan por segundo (ventana fija de 1 s).
    /// Evita progresión descontrolada con autoclickers o teclas repetidas por macros.
    public class ActivityRateLimiter
    {
        public int MaxPerSecond { get; set; } = 8;

        double windowStart = double.NegativeInfinity;
        int count;

        /// now = hora del evento (ActivityEvent.Timestamp), en orden cronológico.
        public bool TryAccept(double now)
        {
            if (MaxPerSecond <= 0) return true;

            if (now - windowStart >= 1.0)
            {
                windowStart = now;
                count = 0;
            }

            if (count >= MaxPerSecond) return false;

            count++;
            return true;
        }
    }
}