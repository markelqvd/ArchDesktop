namespace Archaeo.Platform
{
    /// El tamaño del píxel del arte, común a todo el juego.
    /// Todo el pixel art (mapa, iconos, hoguera, humo, efectos) se dibuja con este tamaño,
    /// para que ningún píxel se vea distinto de otro. Se define en WindowManager (Units Per Art Pixel).
    public static class PixelArt
    {
        /// Valor por defecto: el mapa mide 249 píxeles de ancho y se dibuja a 600 unidades.
        public const float DefaultUnitsPerPixel = 600f / 249f;

        /// Unidades de la UI que mide un píxel del arte.
        public static float UnitsPerPixel =>
            WindowManager.Instance != null ? WindowManager.Instance.UnitsPerArtPixel : DefaultUnitsPerPixel;
    }
}