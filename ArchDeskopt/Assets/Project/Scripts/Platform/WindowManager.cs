using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Archaeo.Platform
{
    /// Un rectángulo de la forma de la ventana, con su propio radio de esquina (en píxeles).
    public readonly struct WindowShapePart
    {
        public readonly RectInt Rect;
        public readonly int Radius;

        public WindowShapePart(RectInt rect, int radius)
        {
            Rect = rect;
            Radius = radius;
        }
    }

    /// Gestiona la ventana del juego en Windows: tamaño, sin bordes,
    /// siempre visible, arrastre con anclaje a los bordes, posición y escala recordadas,
    /// minimizar, cerrar y forma redondeada.
    ///
    /// La ventana de Windows mide SIEMPRE lo mismo: la ventana principal más la zona de
    /// paneles encima. Lo que no se usa se recorta con la forma (SetShape) y queda invisible,
    /// así abrir o cerrar un panel no cambia el tamaño de la ventana ni produce saltos.
    ///
    /// En el Editor no hace nada (para no modificar la ventana de Unity).
    /// Colócalo en un objeto de la escena Boot.
    public class WindowManager : MonoBehaviour
    {
        public static WindowManager Instance { get; private set; }

        [Header("Ventana principal (unidades de referencia)")]
        [SerializeField] int width = 600;
        [SerializeField] int height = 400;
        [SerializeField] bool borderless = true;
        [SerializeField] bool defaultAlwaysOnTop = true;

        [Header("Zona de paneles")]
        [Tooltip("Alto de la zona de paneles. DEBE coincidir con la altura de PanelArea y con Panel Height de PanelHostView.")]
        [SerializeField] int reservedPanelHeight = 320;
        [Tooltip("Radio de las esquinas de la forma inicial, antes de que cargue la interfaz.")]
        [SerializeField] int initialCornerRadius = 14;

        [Header("Anclaje a los bordes")]
        [Tooltip("Distancia (px, a escala 100 %) a la que la ventana se pega al soltarla.")]
        [SerializeField] int snapDistance = 60;
        [Tooltip("Separación (px, a escala 100 %) que queda entre la ventana y el borde.")]
        [SerializeField] int snapMargin = 12;
        [Tooltip("Si está marcado, solo se ancla cuando está cerca de una esquina.")]
        [SerializeField] bool cornersOnly = false;

        const string KeyX = "window_x";
        const string KeyY = "window_y";
        const string KeyTop = "window_always_on_top";
        const string KeyScale = "window_scale";
        const string KeySnap = "window_snap";

        public const float MinScale = 0.5f;
        public const float ScaleStep = 0.25f;

        public bool AlwaysOnTop { get; private set; }
        public float Scale { get; private set; } = 1f;
        public bool SnapToEdges { get; private set; } = true;

        /// Se dispara cuando cambia la escala (la usa el Canvas para escalar la UI).
        public event Action<float> ScaleChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            AlwaysOnTop = PlayerPrefs.GetInt(KeyTop, defaultAlwaysOnTop ? 1 : 0) == 1;
            Scale = Mathf.Clamp(PlayerPrefs.GetFloat(KeyScale, 1f), MinScale, 2f);
            SnapToEdges = PlayerPrefs.GetInt(KeySnap, 1) == 1;
        }

        public void SetSnapToEdges(bool value)
        {
            SnapToEdges = value;
            PlayerPrefs.SetInt(KeySnap, value ? 1 : 0);
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // ───────────── Windows ─────────────
        IntPtr hwnd = IntPtr.Zero;
        bool dragging;
        int dragCursorX, dragCursorY, dragWinX, dragWinY;
        Coroutine snapRoutine;

        int WidthPx => Mathf.RoundToInt(width * Scale);
        int HeightPx => Mathf.RoundToInt(height * Scale);                   // ventana principal
        int PanelPx => Mathf.RoundToInt(reservedPanelHeight * Scale);       // zona de paneles
        int TotalHeightPx => HeightPx + PanelPx;                            // alto real de la ventana de Windows

        /// Mayor escala con la que cabe la ventana (con un panel abierto) en la pantalla principal.
        /// Nunca baja de 1: el 100 % siempre está permitido.
        public float MaxScale
        {
            get
            {
                int screenH = Native.GetSystemMetrics(Native.SM_CYSCREEN);
                float fit = (screenH - 80f) / (height + reservedPanelHeight);
                float stepped = Mathf.Floor(fit / ScaleStep) * ScaleStep;
                return Mathf.Clamp(stepped, 1f, 2f);
            }
        }

        IEnumerator Start()
        {
            // 1. Resolución inicial en modo ventana
            Screen.SetResolution(width, height + reservedPanelHeight, FullScreenMode.Windowed);

            // Esperar a que Unity aplique el cambio antes de tocar la ventana nativa
            yield return null;
            yield return null;

            hwnd = Native.GetActiveWindow();
            if (hwnd == IntPtr.Zero) yield break;

            // Si la escala guardada ya no cabe (otro monitor), reducirla
            Scale = Mathf.Min(Scale, MaxScale);
            ScaleChanged?.Invoke(Scale);

            // 2. Quitar bordes y barra de título
            if (borderless)
            {
                int style = Native.GetWindowLong(hwnd, Native.GWL_STYLE);
                style &= ~(Native.WS_CAPTION | Native.WS_THICKFRAME | Native.WS_SYSMENU | Native.WS_MAXIMIZEBOX);
                Native.SetWindowLong(hwnd, Native.GWL_STYLE, style);
            }

            // 3. Posición (recordada o por defecto), tamaño y z-order.
            //    pos = esquina superior izquierda de la ventana PRINCIPAL; la zona de paneles queda encima.
            Vector2Int pos = LoadPosition();
            Native.SetWindowPos(hwnd, AlwaysOnTop ? Native.HWND_TOPMOST : Native.HWND_NOTOPMOST,
                pos.x, pos.y - PanelPx, WidthPx, TotalHeightPx, Native.SWP_FRAMECHANGED | Native.SWP_SHOWWINDOW);

            // 4. Forma inicial: solo la ventana principal, para no ver la zona de paneles vacía al arrancar
            SetShape(new[] { new RectInt(0, PanelPx, WidthPx, HeightPx) },
                Mathf.RoundToInt(initialCornerRadius * Scale));
        }

        /// Devuelve la esquina superior izquierda de la ventana PRINCIPAL.
        Vector2Int LoadPosition()
        {
            int vx = Native.GetSystemMetrics(Native.SM_XVIRTUALSCREEN);
            int vy = Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN);
            int vw = Native.GetSystemMetrics(Native.SM_CXVIRTUALSCREEN);
            int vh = Native.GetSystemMetrics(Native.SM_CYVIRTUALSCREEN);

            if (PlayerPrefs.HasKey(KeyX) && PlayerPrefs.HasKey(KeyY))
            {
                int x = PlayerPrefs.GetInt(KeyX);
                int y = PlayerPrefs.GetInt(KeyY);
                // Evita que la ventana quede fuera de pantalla (p.ej. si quitaron un monitor)
                x = Mathf.Clamp(x, vx - WidthPx + 100, vx + vw - 100);
                y = Mathf.Clamp(y, vy, vy + vh - 60);
                return new Vector2Int(x, y);
            }

            // Primera vez: esquina derecha de la pantalla principal, con hueco encima para un panel
            int screenW = Native.GetSystemMetrics(Native.SM_CXSCREEN);
            int screenH = Native.GetSystemMetrics(Native.SM_CYSCREEN);
            int top = Mathf.Min(PanelPx + 40, screenH - HeightPx - 60);
            return new Vector2Int(screenW - WidthPx - 20, Mathf.Max(vy, top));
        }

        /// Guarda la posición de la ventana PRINCIPAL (sin contar la zona de paneles).
        void SavePosition()
        {
            if (hwnd == IntPtr.Zero || Native.IsIconic(hwnd)) return; // minimizada: coordenadas inválidas
            if (!Native.GetWindowRect(hwnd, out Native.RECT r)) return;
            PlayerPrefs.SetInt(KeyX, r.Left);
            PlayerPrefs.SetInt(KeyY, r.Top + PanelPx);
            PlayerPrefs.Save();
        }

        /// Área útil del monitor donde está la ventana (sin la barra de tareas).
        bool TryGetWorkArea(out Native.RECT work)
        {
            work = default;
            if (hwnd == IntPtr.Zero) return false;

            var info = new Native.MONITORINFO { cbSize = Marshal.SizeOf(typeof(Native.MONITORINFO)) };
            IntPtr monitor = Native.MonitorFromWindow(hwnd, Native.MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero || !Native.GetMonitorInfo(monitor, ref info)) return false;

            work = info.rcWork;
            return true;
        }

        /// Se llama al abrir (units > 0) o cerrar (0) un panel.
        /// La ventana NO cambia de tamaño: solo se desplaza hacia abajo, si hace falta,
        /// para que el panel no quede por encima del borde superior de la pantalla.
        public void SetExtraHeight(int units)
        {
            if (units <= 0 || hwnd == IntPtr.Zero || Native.IsIconic(hwnd)) return;

            Native.GetWindowRect(hwnd, out Native.RECT r);
            int mainTop = r.Top + PanelPx;
            int panelTop = mainTop - Mathf.Min(PanelPx, Mathf.RoundToInt(units * Scale));

            int limit = TryGetWorkArea(out Native.RECT work)
                ? work.Top
                : Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN);

            if (panelTop >= limit) return;   // cabe

            int shift = limit - panelTop;
            Native.SetWindowPos(hwnd, IntPtr.Zero, r.Left, r.Top + shift, 0, 0,
                Native.SWP_NOSIZE | Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
            SavePosition();
        }

        /// Cambia la escala de la ventana y de la UI, manteniendo la proporción.
        public void SetScale(float value)
        {
            value = Mathf.Clamp(value, MinScale, MaxScale);
            if (Mathf.Approximately(value, Scale)) return;

            int oldPanelPx = PanelPx;
            Scale = value;
            PlayerPrefs.SetFloat(KeyScale, Scale);
            ScaleChanged?.Invoke(Scale);

            if (hwnd == IntPtr.Zero || Native.IsIconic(hwnd)) return;

            Native.GetWindowRect(hwnd, out Native.RECT r);
            int mainTop = r.Top + oldPanelPx;
            int mainLeft = r.Left;

            // Mantener la ventana PRINCIPAL dentro de la pantalla
            int vx = Native.GetSystemMetrics(Native.SM_XVIRTUALSCREEN);
            int vy = Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN);
            int vw = Native.GetSystemMetrics(Native.SM_CXVIRTUALSCREEN);
            int vh = Native.GetSystemMetrics(Native.SM_CYVIRTUALSCREEN);
            if (mainTop + HeightPx > vy + vh) mainTop = vy + vh - HeightPx;
            if (mainTop < vy) mainTop = vy;
            if (mainLeft + WidthPx > vx + vw) mainLeft = vx + vw - WidthPx;
            if (mainLeft < vx) mainLeft = vx;

            Native.SetWindowPos(hwnd, IntPtr.Zero, mainLeft, mainTop - PanelPx, WidthPx, TotalHeightPx,
                Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
            SavePosition();
        }

        /// Recorta la ventana con una unión de rectángulos de esquinas redondeadas,
        /// cada uno con su propio radio. Los rectángulos van en píxeles, relativos a la
        /// esquina superior izquierda de la ventana.
        /// Lo que queda fuera de la forma desaparece y deja pasar los clics a lo de detrás.
        /// Devuelve false si la ventana aún no está lista (hay que reintentarlo).
        public bool SetShape(IReadOnlyList<WindowShapePart> parts)
        {
            if (hwnd == IntPtr.Zero || Native.IsIconic(hwnd)) return false;

            if (parts == null || parts.Count == 0)
            {
                Native.SetWindowRgn(hwnd, IntPtr.Zero, true);   // sin recorte
                return true;
            }

            IntPtr combined = Native.CreateRectRgn(0, 0, 0, 0);

            foreach (WindowShapePart p in parts)
            {
                int diameter = Mathf.Max(0, p.Radius * 2);
                RectInt r = p.Rect;
                IntPtr part = Native.CreateRoundRectRgn(r.xMin, r.yMin, r.xMax + 1, r.yMax + 1, diameter, diameter);
                Native.CombineRgn(combined, combined, part, Native.RGN_OR);
                Native.DeleteObject(part);
            }

            // Tras esta llamada la región pertenece al sistema: no se borra
            Native.SetWindowRgn(hwnd, combined, true);
            return true;
        }

        /// Atajo: todos los rectángulos con el mismo radio.
        public bool SetShape(IReadOnlyList<RectInt> rects, int cornerRadiusPx)
        {
            var parts = new List<WindowShapePart>(rects != null ? rects.Count : 0);
            if (rects != null)
                foreach (RectInt r in rects) parts.Add(new WindowShapePart(r, cornerRadiusPx));
            return SetShape(parts);
        }

        public void BeginDrag()
        {
            if (hwnd == IntPtr.Zero) return;

            // Si se estaba animando un anclaje, cancelarlo
            if (snapRoutine != null) { StopCoroutine(snapRoutine); snapRoutine = null; }

            Native.GetCursorPos(out Native.POINT c);
            Native.GetWindowRect(hwnd, out Native.RECT r);
            dragCursorX = c.X; dragCursorY = c.Y;
            dragWinX = r.Left; dragWinY = r.Top;
            dragging = true;
        }

        public void EndDrag()
        {
            if (!dragging) return;
            dragging = false;

            // Si no hay anclaje, se guarda ya; si lo hay, se guarda al terminar la animación
            if (!TrySnap()) SavePosition();
        }

        // ───────────── Anclaje a los bordes ─────────────
        /// Si la ventana principal está cerca de un borde (o esquina) del monitor, la desliza hasta pegarla.
        bool TrySnap()
        {
            if (!SnapToEdges || hwnd == IntPtr.Zero || Native.IsIconic(hwnd)) return false;
            if (!TryGetWorkArea(out Native.RECT work)) return false;

            Native.GetWindowRect(hwnd, out Native.RECT r);

            int w = WidthPx, h = HeightPx;
            int left = r.Left;
            int top = r.Top + PanelPx;          // borde superior de la ventana principal
            int snap = Mathf.RoundToInt(snapDistance * Scale);
            int margin = Mathf.RoundToInt(snapMargin * Scale);

            int targetLeft = left, targetTop = top;
            bool snapX = false, snapY = false;

            if (Mathf.Abs(left - (work.Left + margin)) <= snap)
            { targetLeft = work.Left + margin; snapX = true; }
            else if (Mathf.Abs((left + w) - (work.Right - margin)) <= snap)
            { targetLeft = work.Right - margin - w; snapX = true; }

            if (Mathf.Abs(top - (work.Top + margin)) <= snap)
            { targetTop = work.Top + margin; snapY = true; }
            else if (Mathf.Abs((top + h) - (work.Bottom - margin)) <= snap)
            { targetTop = work.Bottom - margin - h; snapY = true; }

            // Solo esquinas: hace falta que coincidan los dos ejes
            if (cornersOnly && !(snapX && snapY)) return false;
            if (!snapX && !snapY) return false;
            if (targetLeft == left && targetTop == top) return false;

            snapRoutine = StartCoroutine(AnimateTo(targetLeft, targetTop - PanelPx));
            return true;
        }

        IEnumerator AnimateTo(int toLeft, int toTop)
        {
            const float duration = 0.12f;
            Native.GetWindowRect(hwnd, out Native.RECT r);
            int fromLeft = r.Left, fromTop = r.Top;

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                Native.SetWindowPos(hwnd, IntPtr.Zero,
                    Mathf.RoundToInt(Mathf.Lerp(fromLeft, toLeft, k)),
                    Mathf.RoundToInt(Mathf.Lerp(fromTop, toTop, k)), 0, 0,
                    Native.SWP_NOSIZE | Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
                yield return null;
            }

            Native.SetWindowPos(hwnd, IntPtr.Zero, toLeft, toTop, 0, 0,
                Native.SWP_NOSIZE | Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
            snapRoutine = null;
            SavePosition();
        }

        void Update()
        {
            if (!dragging) return;

            // Si se soltó el botón izquierdo (aunque fuera de la ventana), terminar
            if ((Native.GetAsyncKeyState(Native.VK_LBUTTON) & 0x8000) == 0) { EndDrag(); return; }

            Native.GetCursorPos(out Native.POINT c);
            Native.SetWindowPos(hwnd, IntPtr.Zero,
                dragWinX + c.X - dragCursorX, dragWinY + c.Y - dragCursorY, 0, 0,
                Native.SWP_NOSIZE | Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
        }

        public void SetAlwaysOnTop(bool value)
        {
            AlwaysOnTop = value;
            PlayerPrefs.SetInt(KeyTop, value ? 1 : 0);
            if (hwnd == IntPtr.Zero) return;
            Native.SetWindowPos(hwnd, value ? Native.HWND_TOPMOST : Native.HWND_NOTOPMOST,
                0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
        }

        public void Minimize()
        {
            if (hwnd != IntPtr.Zero) Native.ShowWindow(hwnd, Native.SW_MINIMIZE);
        }

        void OnApplicationQuit() => SavePosition();

        static class Native
        {
            public const int GWL_STYLE = -16;
            public const int WS_CAPTION = 0x00C00000;
            public const int WS_THICKFRAME = 0x00040000;
            public const int WS_SYSMENU = 0x00080000;
            public const int WS_MAXIMIZEBOX = 0x00010000;

            public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
            public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

            public const uint SWP_NOSIZE = 0x0001;
            public const uint SWP_NOMOVE = 0x0002;
            public const uint SWP_NOZORDER = 0x0004;
            public const uint SWP_NOACTIVATE = 0x0010;
            public const uint SWP_FRAMECHANGED = 0x0020;
            public const uint SWP_SHOWWINDOW = 0x0040;

            public const int SW_MINIMIZE = 6;
            public const int VK_LBUTTON = 0x01;
            public const int RGN_OR = 2;
            public const uint MONITOR_DEFAULTTONEAREST = 2;

            public const int SM_CXSCREEN = 0;
            public const int SM_CYSCREEN = 1;
            public const int SM_XVIRTUALSCREEN = 76;
            public const int SM_YVIRTUALSCREEN = 77;
            public const int SM_CXVIRTUALSCREEN = 78;
            public const int SM_CYVIRTUALSCREEN = 79;

            [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
            [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }

            [StructLayout(LayoutKind.Sequential)]
            public struct MONITORINFO
            {
                public int cbSize;
                public RECT rcMonitor;
                public RECT rcWork;     // área útil, sin la barra de tareas
                public uint dwFlags;
            }

            [DllImport("user32.dll")] public static extern IntPtr GetActiveWindow();
            [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
            [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
            [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
            [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
            [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT lpPoint);
            [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
            [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
            [DllImport("user32.dll")] public static extern int GetSystemMetrics(int nIndex);
            [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);
            [DllImport("user32.dll")] public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
            [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
            [DllImport("user32.dll")] public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

            [DllImport("gdi32.dll")] public static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);
            [DllImport("gdi32.dll")] public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int w, int h);
            [DllImport("gdi32.dll")] public static extern int CombineRgn(IntPtr dest, IntPtr src1, IntPtr src2, int mode);
            [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObject);
        }
#else
        // ───────────── Editor / otras plataformas: sin efecto ─────────────
        public float MaxScale => 1f;

        IEnumerator Start()
        {
            Screen.SetResolution(width, height + reservedPanelHeight, FullScreenMode.Windowed);
            yield break;
        }

        public void BeginDrag() { }
        public void EndDrag() { }
        public void SetAlwaysOnTop(bool value) { AlwaysOnTop = value; }
        public void Minimize() { }
        public void SetExtraHeight(int units) { }
        public void SetScale(float value) { }
        public bool SetShape(IReadOnlyList<RectInt> rects, int cornerRadiusPx) => true;
        public bool SetShape(IReadOnlyList<WindowShapePart> parts) => true;
#endif

        public void Quit() => Application.Quit();
    }
}