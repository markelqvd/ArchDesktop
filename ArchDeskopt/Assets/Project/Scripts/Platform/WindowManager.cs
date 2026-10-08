using System;
using System.Collections;
using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Archaeo.Platform
{
    /// Gestiona la ventana del juego en Windows: tamaño, sin bordes,
    /// siempre visible, arrastre, posición recordada, minimizar y cerrar.
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

        const string KeyX = "window_x";
        const string KeyY = "window_y";
        const string KeyTop = "window_always_on_top";

        public bool AlwaysOnTop { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // ───────────── Windows ─────────────
        IntPtr hwnd = IntPtr.Zero;
        bool dragging;
        int dragCursorX, dragCursorY, dragWinX, dragWinY;

        IEnumerator Start()
        {
            // 1. Resolución inicial en modo ventana
            Screen.SetResolution(width, height, FullScreenMode.Windowed);

            // Esperar a que Unity aplique el cambio antes de tocar la ventana nativa
            yield return null;
            yield return null;

            hwnd = Native.GetActiveWindow();
            if (hwnd == IntPtr.Zero) yield break;

            AlwaysOnTop = PlayerPrefs.GetInt(KeyTop, defaultAlwaysOnTop ? 1 : 0) == 1;

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
                pos.x, pos.y, width, height, Native.SWP_FRAMECHANGED | Native.SWP_SHOWWINDOW);
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
                x = Mathf.Clamp(x, vx - width + 100, vx + vw - 100);
                y = Mathf.Clamp(y, vy, vy + vh - 60);
                return new Vector2Int(x, y);
            }

            // Primera vez: esquina superior derecha de la pantalla principal
            int screenW = Native.GetSystemMetrics(Native.SM_CXSCREEN);
            return new Vector2Int(screenW - width - 20, 40);
        }

        void SavePosition()
        {
            if (hwnd == IntPtr.Zero || Native.IsIconic(hwnd)) return; // minimizada: coordenadas inválidas
            if (!Native.GetWindowRect(hwnd, out Native.RECT r)) return;
            PlayerPrefs.SetInt(KeyX, r.Left);
            PlayerPrefs.SetInt(KeyY, r.Top);
            PlayerPrefs.Save();
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

            public const int SM_CXSCREEN = 0;
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
        }
#else
        // ───────────── Editor / otras plataformas: sin efecto ─────────────
        IEnumerator Start()
        {
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            yield break;
        }

        public void BeginDrag() { }
        public void EndDrag() { }
        public void SetAlwaysOnTop(bool value) { AlwaysOnTop = value; }
        public void Minimize() { }
#endif

        public void Quit() => Application.Quit();
    }
}