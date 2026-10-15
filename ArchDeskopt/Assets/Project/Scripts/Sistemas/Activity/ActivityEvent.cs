using System.Diagnostics;

namespace Archaeo.Activity
{
    public enum ActivityKind { Mouse, Keyboard }

    /// "Ha habido actividad". Nada más.
    /// PRIVACIDAD: este tipo no tiene ningún campo para la tecla pulsada, el texto
    /// escrito ni la aplicación en uso. Si no puede guardarse aquí, no se guarda.
    public readonly struct ActivityEvent
    {
        public readonly ActivityKind Kind;
        public readonly double Timestamp;   // segundos, reloj monotónico

        public ActivityEvent(ActivityKind kind, double timestamp)
        {
            Kind = kind;
            Timestamp = timestamp;
        }

        /// Hora actual con el mismo reloj para cualquier hilo.
        public static double Now => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
    }
}