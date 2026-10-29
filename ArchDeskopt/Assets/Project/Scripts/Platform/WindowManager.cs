using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Archaeo.Platform
{
    /// Una pieza de la forma de la ventana, en píxeles relativos a la esquina superior izquierda de la ventana.
    /// Por defecto es un rectángulo con esquinas redondeadas (Radius). Si trae Mask, la pieza toma la
    /// silueta de esa lista de rectángulos (relativos a Rect), por ejemplo la transparencia de un sprite.
    public readonly struct WindowShapePart
    {
        public readonly RectInt Rect;
        public readonly int Radius;
        public readonly IReadOnlyList<RectInt> Mask;   // null = rectángulo redondeado
        public readonly int MaskKey;                   // identifica la máscara, para saber si cambió

        public WindowShapePart(RectInt rect, int radius) : this(rect, radius, null, 0) { }

        public WindowShapePart(RectInt rect, int radius, IReadOnlyList<RectInt> mask, int maskKey)
        {
            Rect = rect;
            Radius = radius;
            Mask = mask;
            MaskKey = maskKey;
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

        [Tooltip("Unidades de la UI que mide cada píxel del arte. Es el tamaño del mapa: su ancho en unidades (600) entre su ancho en píxeles (249). Si cambia el mapa, cámbialo aquí.")]
        [SerializeField] float unitsPerArtPixel = 600f / 249f;
        [SerializeField] bool borderless = true;
        [SerializeField] bool defaultAlwaysOnTop = true;

        [Tooltip("La primera vez que se abre el juego, la ventana principal ocupa esta fracción de la altura de la pantalla (se redondea a múltiplos de 25 % para que los píxeles del arte midan siempre un número entero). 0 = 100 %.")]
        [SerializeField, Range(0f, 0.6f)] float firstRunHeightFraction = 0.25f;

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
        const string KeyPixelSize = "window_pixel_size";
        const string KeySnap = "window_snap";

        /// Unidades de la UI por píxel del arte (ver PixelArt).
        public float UnitsPerArtPixel => unitsPerArtPixel;

        public bool AlwaysOnTop { get; private set; }
        /// Píxeles de pantalla que mide cada píxel del arte (1, 2, 3...). Siempre un número entero.
        public int PixelSize { get; private set; } = 2;
        /// Escala de la UI que resulta de PixelSize (la usa el Canvas).
        public float Scale { get; private set; } = 0.83f;
        public bool SnapToEdges { get; private set; } = true;

        bool hasSavedScale;   // false la primera vez que se abre el juego

        /// Se dispara cuando cambia la escala (la usa el Canvas para escalar la UI).
        public event Action<float> ScaleChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            AlwaysOnTop = PlayerPrefs.GetInt(KeyTop, defaultAlwaysOnTop ? 1 : 0) == 1;
            hasSavedScale = PlayerPrefs.HasKey(KeyPixelSize);
            PixelSize = Mathf.Max(1, PlayerPrefs.GetInt(KeyPixelSize, 2));
            Scale = PixelSize / unitsPerArtPixel;
            SnapToEdges = PlayerPrefs.GetInt(KeySnap, 1) == 1;
        }

        public void SetSnapToEdges(bool value)
        {
            SnapToEdges = value;
            PlayerPrefs.SetInt(KeySnap, value ? 1 : 0);
        }

        /// Borra el tamaño, la posición y las opciones de ventana guardados (menú de los tres puntos del componente).
        [ContextMenu("Restablecer tamaño y posición guardados")]
        void ResetSavedWindow()
        {
            PlayerPrefs.DeleteKey(KeyX);
            PlayerPrefs.DeleteKey(KeyY);
            PlayerPrefs.DeleteKey(KeyScale);
            PlayerPrefs.DeleteKey(KeyPixelSize);
            PlayerPrefs.DeleteKey(KeyTop);
            PlayerPrefs.DeleteKey(KeySnap);
            PlayerPrefs.Save();
            Debug.Log("[Ventana] Tamaño y posición guardados borrados: la próxima vez se usarán los valores por defecto.");
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // ───────────── Windows ─────────────
        IntPtr hwnd = IntPtr.Zero;
        bool dragging;
        int dragCursorX, dragCursorY, dragWinX, dragWinY;
        Coroutine snapRoutine;
        bool uiShapeApplied;   // true cuando la interfaz ya ha aplicado su forma

        int WidthPx => Mathf.RoundToInt(width * Scale);
        int HeightPx => Mathf.RoundToInt(height * Scale);                   // ventana principal
        int PanelPx => Mathf.RoundToInt(reservedPanelHeight * Scale);       // zona de paneles
        int TotalHeightPx => HeightPx + PanelPx;                            // alto real de la ventana de Windows

        /// Mayor tamaño de píxel con el que cabe la ventana (con un panel abierto) en la pantalla principal.
        /// Nunca baja de 2: ×2 siempre está permitido.
        public int MaxPixelSize
        {
            get
            {
                int screenH = Native.GetSystemMetrics(Native.SM_CYSCREEN);
                float totalUnits = height + reservedPanelHeight;
                int fit = Mathf.FloorToInt((screenH - 80f) * unitsPerArtPixel / totalUnits);
                return Mathf.Max(2, fit);
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

            // Primera vez: tamaño inicial según la pantalla (un entero: cada píxel del arte mide N píxeles de pantalla)
            if (!hasSavedScale && firstRunHeightFraction > 0f)
            {
                int screenH = Native.GetSystemMetrics(Native.SM_CYSCREEN);
                float ideal = screenH * firstRunHeightFraction * unitsPerArtPixel / height;
                PixelSize = Mathf.RoundToInt(ideal);
            }

            // Dentro de los límites de esta pantalla (por si cambió de monitor)
            PixelSize = Mathf.Clamp(PixelSize, 1, MaxPixelSize);
            Scale = PixelSize / unitsPerArtPixel;
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

            // 4. La ventana queda invisible (forma mínima) hasta que la interfaz aplique la suya,
            //    para que no se vea ningún color de fondo mientras carga.
            ApplyShape(new[] { new WindowShapePart(new RectInt(0, 0, 1, 1), 0) });
            StartCoroutine(FallbackShape());
        }

        /// Si la interfaz no aplica ninguna forma en unos segundos, se usa un rectángulo redondeado
        /// para que la ventana no se quede invisible.
        IEnumerator FallbackShape()
        {
            yield return new WaitForSecondsRealtime(5f);
            if (uiShapeApplied) yield break;

            Debug.LogWarning("[Ventana] La interfaz no aplicó ninguna forma: se usa la forma redondeada por defecto.");
            ApplyShape(new[]
            {
                new WindowShapePart(new RectInt(0, PanelPx, WidthPx, HeightPx),
                    Mathf.RoundToInt(initialCornerRadius * Scale))
            });
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

        /// Cambia el tamaño de la ventana y de la UI: cada píxel del arte pasa a medir "value" píxeles de pantalla.
        public void SetPixelSize(int value)
        {
            value = Mathf.Clamp(value, 1, MaxPixelSize);
            if (value == PixelSize) return;

            int oldPanelPx = PanelPx;
            PixelSize = value;
            Scale = PixelSize / unitsPerArtPixel;
            PlayerPrefs.SetInt(KeyPixelSize, PixelSize);
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

        /// Recorta la ventana con la unión de las piezas indicadas (rectángulos redondeados o siluetas de imagen).
        /// Lo que queda fuera de la forma desaparece y deja pasar los clics a lo de detrás.
        /// Devuelve false si la ventana aún no está lista (hay que reintentarlo).
        public bool SetShape(IReadOnlyList<WindowShapePart> parts)
        {
            if (!ApplyShape(parts)) return false;
            uiShapeApplied = true;
            return true;
        }

        bool ApplyShape(IReadOnlyList<WindowShapePart> parts)
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
                IntPtr region = (p.Mask != null && p.Mask.Count > 0)
                    ? RegionFromMask(p.Mask, p.Rect.xMin, p.Rect.yMin)
                    : RoundedRegion(p);

                if (region == IntPtr.Zero) continue;
                Native.CombineRgn(combined, combined, region, Native.RGN_OR);
                Native.DeleteObject(region);
            }

            // Tras esta llamada la región pertenece al sistema: no se borra
            Native.SetWindowRgn(hwnd, combined, true);
            return true;
        }

        static IntPtr RoundedRegion(WindowShapePart p)
        {
            int diameter = Mathf.Max(0, p.Radius * 2);
            RectInt r = p.Rect;
            return Native.CreateRoundRectRgn(r.xMin, r.yMin, r.xMax + 1, r.yMax + 1, diameter, diameter);
        }

        /// Crea una región a partir de una lista de rectángulos (ordenados de arriba abajo y de izquierda a derecha).
        static IntPtr RegionFromMask(IReadOnlyList<RectInt> rects, int offsetX, int offsetY)
        {
            int count = rects.Count;
            int size = 32 + count * 16;               // cabecera RGNDATAHEADER + un RECT por rectángulo
            IntPtr buffer = Marshal.AllocHGlobal(size);

            try
            {
                int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;

                for (int i = 0; i < count; i++)
                {
                    RectInt r = rects[i];
                    int left = r.xMin + offsetX, top = r.yMin + offsetY;
                    int right = r.xMax + offsetX, bottom = r.yMax + offsetY;

                    int at = 32 + i * 16;
                    Marshal.WriteInt32(buffer, at, left);
                    Marshal.WriteInt32(buffer, at + 4, top);
                    Marshal.WriteInt32(buffer, at + 8, right);
                    Marshal.WriteInt32(buffer, at + 12, bottom);

                    if (left < minX) minX = left;
                    if (top < minY) minY = top;
                    if (right > maxX) maxX = right;
                    if (bottom > maxY) maxY = bottom;
                }

                Marshal.WriteInt32(buffer, 0, 32);          // dwSize
                Marshal.WriteInt32(buffer, 4, 1);           // iType = RDH_RECTANGLES
                Marshal.WriteInt32(buffer, 8, count);       // nCount
                Marshal.WriteInt32(buffer, 12, count * 16); // nRgnSize
                Marshal.WriteInt32(buffer, 16, minX);       // rcBound
                Marshal.WriteInt32(buffer, 20, minY);
                Marshal.WriteInt32(buffer, 24, maxX);
                Marshal.WriteInt32(buffer, 28, maxY);

                return Native.ExtCreateRegion(IntPtr.Zero, (uint)size, buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
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
            [DllImport("gdi32.dll")] public static extern IntPtr ExtCreateRegion(IntPtr lpx, uint nCount, IntPtr lpData);
            [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObject);
        }
#else
        // ───────────── Editor / otras plataformas: sin efecto ─────────────
        public int MaxPixelSize => 2;

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
        public void SetPixelSize(int value) { }
        public bool SetShape(IReadOnlyList<RectInt> rects, int cornerRadiusPx) => true;
        public bool SetShape(IReadOnlyList<WindowShapePart> parts) => true;
#endif

        public void Quit() => Application.Quit();
    }
}