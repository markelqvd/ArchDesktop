using System;
using System.Collections.Concurrent;
using Archaeo.Activity;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
using System.Threading;
#endif

namespace Archaeo.Platform
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    /// Detecta clics y pulsaciones en todo el sistema, aunque el juego no tenga el foco.
    ///
    /// Funciona consultando cada 10 ms si un botón/tecla ha pasado de "levantado" a
    /// "pulsado" (no instala ningún hook global). Mantener una tecla pulsada cuenta UNA vez.
    ///
    /// PRIVACIDAD: el estado de las teclas se usa solo para detectar el cambio y se descarta.
    /// Lo único que sale de aquí es un ActivityEvent (tipo + hora). Nunca se guarda qué tecla fue.
    public sealed class GlobalInputPoller : IDisposable
    {
        const int PollIntervalMs = 10;
        const int KeyDownMask = 0x8000;
        const int FirstKey = 0x08;   // desde Backspace
        const int LastKey = 0xFE;

        // Botones del ratón: izquierdo, derecho, central, X1, X2
        static readonly int[] MouseButtons = { 0x01, 0x02, 0x04, 0x05, 0x06 };

        readonly ConcurrentQueue<ActivityEvent> output;
        Thread thread;
        volatile bool running;

        public GlobalInputPoller(ConcurrentQueue<ActivityEvent> output)
        {
            this.output = output;
        }

        public void Start()
        {
            if (running) return;
            running = true;
            thread = new Thread(Run) { IsBackground = true, Name = "ActivityPoller" };
            thread.Start();
        }

        public void Dispose()
        {
            running = false;
            thread?.Join(200);
            thread = null;
        }

        void Run()
        {
            var mouseWasDown = new bool[MouseButtons.Length];
            var keyWasDown = new bool[LastKey + 1];

            // Estado inicial: lo que ya esté pulsado al arrancar no cuenta como evento
            for (int i = 0; i < MouseButtons.Length; i++) mouseWasDown[i] = IsDown(MouseButtons[i]);
            for (int vk = FirstKey; vk <= LastKey; vk++) keyWasDown[vk] = IsDown(vk);

            while (running)
            {
                for (int i = 0; i < MouseButtons.Length; i++)
                {
                    bool down = IsDown(MouseButtons[i]);
                    if (down && !mouseWasDown[i]) Emit(ActivityKind.Mouse);
                    mouseWasDown[i] = down;
                }

                for (int vk = FirstKey; vk <= LastKey; vk++)
                {
                    bool down = IsDown(vk);
                    if (down && !keyWasDown[vk]) Emit(ActivityKind.Keyboard);
                    keyWasDown[vk] = down;
                }

                Thread.Sleep(PollIntervalMs);
            }
        }

        static bool IsDown(int vk) => (GetAsyncKeyState(vk) & KeyDownMask) != 0;

        void Emit(ActivityKind kind) => output.Enqueue(new ActivityEvent(kind, ActivityEvent.Now));

        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);
    }
#else
    /// Otras plataformas: sin detección por ahora.
    public sealed class GlobalInputPoller : IDisposable
    {
        public GlobalInputPoller(ConcurrentQueue<ActivityEvent> output) { }
        public void Start() { }
        public void Dispose() { }
    }
#endif
}