using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Archaeo.Platform
{
    /// Gestiona la ventana del juego en Windows: tamaño, sin bordes,
    /// siempre visible, arrastre, posición y escala recordadas, minimizar,
    /// cerrar, crecimiento hacia arriba para los paneles y forma redondeada.
    /// En el Editor no hace nada (para no modificar la ventana de Unity).
    /// Colócalo en un objeto de la escena Boot.
    public class WindowManager : MonoBehaviour
    {
        public static WindowManager Instance { get; private set; }

        [Header("Ventana")]
        [SerializeField] int width = 480;
        [SerializeField] int height = 640;
        [SerializeField] bool borderless = true;
        [SerializeField] bool defaultAlwaysOnTop = true;

        [Tooltip("Alto máximo de la zona de paneles (unidades). Sirve para calcular el tamaño máximo que cabe en pantalla.")]
        [SerializeField] int reservedPanelHeight = 400;

        const string KeyX = "window_x";
        const string KeyY = "window_y";
        const string KeyTop = "window_always_on_top";
        const string KeyScale = "window_scale";

        public const float MinScale = 0.5f;
        public const float ScaleStep = 0.25f;

        public bool AlwaysOnTop { get; private set; }
        public float Scale { get; private set; } = 1f;

        /// Se dispara cuando cambia la escala (la usa el Canvas para escalar la UI).
        public event Action<float> ScaleChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            AlwaysOnTop = PlayerPrefs.GetInt(KeyTop, defaultAlwaysOnTop ? 1 : 0) == 1;
            Scale = Mathf.Clamp(PlayerPrefs.GetFloat(KeyScale, 1f), MinScale, 2f);
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // ───────────── Windows ─────────────
        IntPtr hwnd = IntPtr.Zero;
        bool dragging;
        int dragCursorX, dragCursorY, dragWinX, dragWinY;

        int extraUnits;     // alto extra de la zona de paneles, en unidades de referencia
        int extraPx;        // ese mismo alto extra, en píxeles

        int WidthPx => Mathf.RoundToInt(width * Scale);
        int HeightPx => Mathf.RoundToInt(height * Scale);

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
            Screen.SetResolution(width, height, FullScreenMode.Windowed);

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

            // 3. Posición (recordada o por defecto), tamaño y z-order
            Vector2Int pos = LoadPosition();
            Native.SetWindowPos(hwnd, AlwaysOnTop ? Native.HWND_TOPMOST : Native.HWND_NOTOPMOST,
                pos.x, pos.y, WidthPx, HeightPx, Native.SWP_FRAMECHANGED | Native.SWP_SHOWWINDOW);
        }

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

            // Primera vez: esquina superior derecha de la pantalla principal
            int screenW = Native.GetSystemMetrics(Native.SM_CXSCREEN);
            return new Vector2Int(screenW - WidthPx - 20, 40);
        }

        /// Guarda la posición de la ventana PRINCIPAL (sin contar la zona de paneles).
        void SavePosition()
        {
            if (hwnd == IntPtr.Zero || Native.IsIconic(hwnd)) return; // minimizada: coordenadas inválidas
            if (!Native.GetWindowRect(hwnd, out Native.RECT r)) return;
            PlayerPrefs.SetInt(KeyX, r.Left);
            PlayerPrefs.SetInt(KeyY, r.Top + extraPx);
            PlayerPrefs.Save();
        }

        /// Hace crecer (o encoger) la ventana hacia arriba.
        /// units = alto extra en unidades de referencia del Canvas (0 = sin zona de paneles).
        public void SetExtraHeight(int units)
        {
            if (hwnd == IntPtr.Zero || Native.IsIconic(hwnd)) return;

            int newExtra = Mathf.Max(0, Mathf.RoundToInt(units * Scale));
            if (newExtra == extraPx && units == extraUnits) return;

            Native.GetWindowRect(hwnd, out Native.RECT r);
            int mainTop = r.Top + extraPx;   // borde superior de la ventana principal
            int newTop = mainTop - newExtra;

            // Si no cabe por arriba, bajar la ventana lo justo
            int vy = Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN);
            if (newTop < vy) newTop = vy;

            extraUnits = Mathf.Max(0, units);
            extraPx = newExtra;
            Native.SetWindowPos(hwnd, IntPtr.Zero, r.Left, newTop, WidthPx, HeightPx + extraPx,
                Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
        }

        /// Cambia la escala de la ventana y de la UI, manteniendo la proporción.
        public void SetScale(float value)
        {
            value = Mathf.Clamp(value, MinScale, MaxScale);
            if (Mathf.Approximately(value, Scale)) return;

            Scale = value;
            PlayerPrefs.SetFloat(KeyScale, Scale);
            ScaleChanged?.Invoke(Scale);

            if (hwnd == IntPtr.Zero || Native.IsIconic(hwnd)) return;

            Native.GetWindowRect(hwnd, out Native.RECT r);
            int mainTop = r.Top + extraPx;                       // con el extra anterior
            extraPx = Mathf.RoundToInt(extraUnits * Scale);      // extra con la nueva escala
            int totalH = HeightPx + extraPx;

            int newLeft = r.Left;
            int newTop = mainTop - extraPx;

            // Mantener la ventana dentro de la pantalla
            int vx = Native.GetSystemMetrics(Native.SM_XVIRTUALSCREEN);
            int vy = Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN);
            int vw = Native.GetSystemMetrics(Native.SM_CXVIRTUALSCREEN);
            int vh = Native.GetSystemMetrics(Native.SM_CYVIRTUALSCREEN);
            if (newTop + totalH > vy + vh) newTop = vy + vh - totalH;
            if (newTop < vy) newTop = vy;
            if (newLeft + WidthPx > vx + vw) newLeft = vx + vw - WidthPx;
            if (newLeft < vx) newLeft = vx;

            Native.SetWindowPos(hwnd, IntPtr.Zero, newLeft, newTop, WidthPx, totalH,
                Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
            SavePosition();
        }

        /// Recorta la ventana con una unión de rectángulos de esquinas redondeadas.
        /// rects: en píxeles, relativos a la esquina superior izquierda de la ventana.
        /// Lo que queda fuera de la forma desaparece y deja pasar los clics a lo de detrás.
        /// Devuelve false si la ventana aún no está lista (hay que reintentarlo).
        public bool SetShape(IReadOnlyList<RectInt> rects, int cornerRadiusPx)
        {
            if (hwnd == IntPtr.Zero || Native.IsIconic(hwnd)) return false;

            if (rects == null || rects.Count == 0)
            {
                Native.SetWindowRgn(hwnd, IntPtr.Zero, true);   // sin recorte
                return true;
            }

            int diameter = Mathf.Max(0, cornerRadiusPx * 2);
            IntPtr combined = Native.CreateRectRgn(0, 0, 0, 0);

            foreach (RectInt r in rects)
            {
                IntPtr part = Native.CreateRoundRectRgn(r.xMin, r.yMin, r.xMax + 1, r.yMax + 1, diameter, diameter);
                Native.CombineRgn(combined, combined, part, Native.RGN_OR);
                Native.DeleteObject(part);
            }

            // Tras esta llamada la región pertenece al sistema: no se borra
            Native.SetWindowRgn(hwnd, combined, true);
            return true;
        }

        public void BeginDrag()
        {
            if (hwnd == IntPtr.Zero) return;
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

            public const int SM_CXSCREEN = 0;
            public const int SM_CYSCREEN = 1;
            public const int SM_XVIRTUALSCREEN = 76;
            public const int SM_YVIRTUALSCREEN = 77;
            public const int SM_CXVIRTUALSCREEN = 78;
            public const int SM_CYVIRTUALSCREEN = 79;

            [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
            [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }

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
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            yield break;
        }

        public void BeginDrag() { }
        public void EndDrag() { }
        public void SetAlwaysOnTop(bool value) { AlwaysOnTop = value; }
        public void Minimize() { }
        public void SetExtraHeight(int units) { }
        public void SetScale(float value) { }
        public bool SetShape(IReadOnlyList<RectInt> rects, int cornerRadiusPx) => true;
#endif

        public void Quit() => Application.Quit();
    }
}