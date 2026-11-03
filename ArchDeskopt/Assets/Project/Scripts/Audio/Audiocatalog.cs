namespace Archaeo.Sound
{
    /// Definición de un evento de sonido: valores iniciales con los que se crea su asset.
    public readonly struct SoundEventDef
    {
        public readonly string Id, Description;
        public readonly AudioBus Bus;
        public readonly float VolumeDb, Cooldown, DuckMusicDb, DuckSeconds;
        public readonly int MaxInstances, Priority;
        public readonly bool Loop, PlayInBackground;

        public SoundEventDef(string id, AudioBus bus, string description, float volumeDb, float cooldown, int maxInstances,
            bool loop, bool playInBackground, float duckMusicDb, float duckSeconds, int priority)
        {
            Id = id; Bus = bus; Description = description; VolumeDb = volumeDb; Cooldown = cooldown;
            MaxInstances = maxInstances; Loop = loop; PlayInBackground = playInBackground;
            DuckMusicDb = duckMusicDb; DuckSeconds = duckSeconds; Priority = priority;
        }
    }

    public readonly struct MusicTrackDef
    {
        public readonly string Id, Description;
        public MusicTrackDef(string id, string description) { Id = id; Description = description; }
    }

    /// Catálogo de todos los sonidos del juego. Para añadir uno nuevo: añade aquí su definición y su id en AudioIds,
    /// y usa el menú Archaeo > Audio > 1. Crear o completar eventos de sonido.
    public static class AudioCatalog
    {
        public static readonly SoundEventDef[] Events =
        {
            new SoundEventDef(AudioIds.UI_Click, AudioBus.Ui, "Clic en cualquier botón de la interfaz.", 0f, 0.03f, 4, false, true, 0f, 0f, 128),
            new SoundEventDef(AudioIds.UI_Hover, AudioBus.Ui, "El ratón pasa por encima de un botón. Muy suave.", -10f, 0.05f, 2, false, true, 0f, 0f, 170),
            new SoundEventDef(AudioIds.UI_PanelOpen, AudioBus.Ui, "Se abre un panel (Museo, Tienda, Localizaciones o Ajustes).", 0f, 0.1f, 1, false, true, 0f, 0f, 128),
            new SoundEventDef(AudioIds.UI_PanelClose, AudioBus.Ui, "Se cierra un panel.", 0f, 0.1f, 1, false, true, 0f, 0f, 128),
            new SoundEventDef(AudioIds.UI_Toggle, AudioBus.Ui, "Se marca o desmarca una casilla en Ajustes.", 0f, 0.05f, 2, false, true, 0f, 0f, 128),
            new SoundEventDef(AudioIds.UI_Toast, AudioBus.Ui, "Aparece un aviso pequeño de texto en la ventana.", 0f, 0.2f, 1, false, true, 0f, 0f, 128),
            new SoundEventDef(AudioIds.UI_BuyUpgrade, AudioBus.Ui, "Se compra una mejora en la tienda.", 0f, 0.1f, 1, false, true, 0f, 0f, 100),
            new SoundEventDef(AudioIds.UI_BuyDenied, AudioBus.Ui, "Se intenta comprar una mejora sin monedas suficientes.", 0f, 0.2f, 1, false, true, 0f, 0f, 110),
            new SoundEventDef(AudioIds.Activity_Click, AudioBus.Sfx, "El jugador hace un clic, en cualquier programa. Suena muy a menudo: debe ser corto y muy suave.", -14f, 0.07f, 3, false, false, 0f, 0f, 220),
            new SoundEventDef(AudioIds.Activity_Key, AudioBus.Sfx, "El jugador pulsa una tecla, en cualquier programa. Suena muy a menudo: debe ser corto y muy suave.", -16f, 0.07f, 3, false, false, 0f, 0f, 220),
            new SoundEventDef(AudioIds.Excavation_Find, AudioBus.Sfx, "La barra de excavación se llena: ha aparecido algo (todavía no se sabe qué).", 0f, 0.5f, 1, false, true, 0f, 0f, 60),
            new SoundEventDef(AudioIds.Excavation_EarlyFind, AudioBus.Sfx, "Hallazgo por suerte, antes de llenar la barra.", 0f, 0.5f, 1, false, true, 0f, 0f, 60),
            new SoundEventDef(AudioIds.Artifact_New, AudioBus.Sfx, "Sale un objeto NUEVO. Es el sonido general: se usa si el de su rareza está vacío.", 0f, 0.5f, 1, false, true, -6f, 2f, 40),
            new SoundEventDef(AudioIds.Artifact_New_Common, AudioBus.Sfx, "Sale un objeto nuevo de rareza Común. Si no tiene sonido, suena Artifact_New.", 0f, 0.5f, 1, false, true, -4f, 1.5f, 40),
            new SoundEventDef(AudioIds.Artifact_New_Uncommon, AudioBus.Sfx, "Sale un objeto nuevo de rareza Poco común. Si no tiene sonido, suena Artifact_New.", 0f, 0.5f, 1, false, true, -5f, 1.8f, 40),
            new SoundEventDef(AudioIds.Artifact_New_Rare, AudioBus.Sfx, "Sale un objeto nuevo de rareza Raro. Si no tiene sonido, suena Artifact_New.", 0f, 0.5f, 1, false, true, -6f, 2.2f, 40),
            new SoundEventDef(AudioIds.Artifact_New_VeryRare, AudioBus.Sfx, "Sale un objeto nuevo de rareza Muy raro. Si no tiene sonido, suena Artifact_New.", 0f, 0.5f, 1, false, true, -7f, 2.5f, 40),
            new SoundEventDef(AudioIds.Artifact_New_Relic, AudioBus.Sfx, "Sale un objeto nuevo de rareza Reliquia. Si no tiene sonido, suena Artifact_New.", 0f, 0.5f, 1, false, true, -8f, 3f, 40),
            new SoundEventDef(AudioIds.Artifact_Duplicate, AudioBus.Sfx, "Sale un objeto que ya tenías y sube de nivel. Discreto: no debe interrumpir.", -6f, 0.3f, 1, false, true, 0f, 0f, 120),
            new SoundEventDef(AudioIds.Artifact_Restored, AudioBus.Sfx, "Se completa una restauración y el objeto pasa al museo.", 0f, 0.5f, 1, false, true, -6f, 2f, 40),
            new SoundEventDef(AudioIds.Clean_Start, AudioBus.Sfx, "Se abre el minijuego de limpiar.", 0f, 0.2f, 1, false, true, 0f, 0f, 80),
            new SoundEventDef(AudioIds.Clean_Scrub, AudioBus.Sfx, "Mientras el jugador limpia con el ratón. Es un bucle: suena solo mientras frota.", -6f, 0f, 1, true, true, 0f, 0f, 90),
            new SoundEventDef(AudioIds.Clean_Complete, AudioBus.Sfx, "Se termina de limpiar el objeto.", 0f, 0.3f, 1, false, true, 0f, 0f, 60),
            new SoundEventDef(AudioIds.Clean_Fail, AudioBus.Sfx, "Se acaba el tiempo del minijuego sin completarlo.", 0f, 0.3f, 1, false, true, 0f, 0f, 80),
            new SoundEventDef(AudioIds.Minigame_TimerWarning, AudioBus.Sfx, "Quedan 5 segundos o menos en un minijuego (un tic por segundo).", 0f, 0.5f, 1, false, true, 0f, 0f, 70),
            new SoundEventDef(AudioIds.Examine_Start, AudioBus.Sfx, "Se abre el minijuego de examinar.", 0f, 0.2f, 1, false, true, 0f, 0f, 80),
            new SoundEventDef(AudioIds.Examine_PointHit, AudioBus.Sfx, "El jugador acierta uno de los puntos. Conviene tener varias variaciones.", 0f, 0.03f, 4, false, true, 0f, 0f, 90),
            new SoundEventDef(AudioIds.Examine_Complete, AudioBus.Sfx, "Se aciertan todos los puntos del minijuego de examinar.", 0f, 0.3f, 1, false, true, 0f, 0f, 60),
            new SoundEventDef(AudioIds.Examine_Fail, AudioBus.Sfx, "Se acaba el tiempo del minijuego de examinar sin completarlo.", 0f, 0.3f, 1, false, true, 0f, 0f, 80),
            new SoundEventDef(AudioIds.Map_Transition, AudioBus.Sfx, "El mapa cambia de capa con la nube de polvo.", 0f, 1f, 1, false, true, -4f, 1.8f, 70),
            new SoundEventDef(AudioIds.Effect_PickHit, AudioBus.Sfx, "Un picazo de la animación del pico sobre el mapa.", -10f, 0.1f, 3, false, false, 0f, 0f, 200),
            new SoundEventDef(AudioIds.Effect_BrushSweep, AudioBus.Sfx, "Un barrido de la animación de la brocha sobre el mapa.", -10f, 0.1f, 2, false, false, 0f, 0f, 200),
            new SoundEventDef(AudioIds.Ambient_Zone1, AudioBus.Ambience, "Ambiente de la zona 1 (viento, pájaros...). Bucle continuo.", -8f, 0f, 1, true, false, 0f, 0f, 230),
            new SoundEventDef(AudioIds.Ambient_Campfire, AudioBus.Ambience, "Crepitar de la hoguera. Solo suena con la capa 2 del mapa. Bucle.", -10f, 0f, 1, true, false, 0f, 0f, 225),
            new SoundEventDef(AudioIds.Economy_OfflineIncome, AudioBus.Sfx, "Al abrir el juego: el museo ha generado monedas mientras estabas fuera.", 0f, 1f, 1, false, true, 0f, 0f, 70),
            new SoundEventDef(AudioIds.Window_Start, AudioBus.Sfx, "El juego se abre.", 0f, 1f, 1, false, true, 0f, 0f, 100),
            new SoundEventDef(AudioIds.Window_Minimize, AudioBus.Sfx, "Se minimiza la ventana.", 0f, 0.3f, 1, false, true, 0f, 0f, 128),
            new SoundEventDef(AudioIds.Window_Snap, AudioBus.Sfx, "La ventana se pega a un borde de la pantalla.", -4f, 0.3f, 1, false, true, 0f, 0f, 150),
            new SoundEventDef(AudioIds.Window_Quit, AudioBus.Sfx, "Se cierra el juego.", 0f, 1f, 1, false, true, 0f, 0f, 100),
        };

        public static readonly MusicTrackDef[] Tracks =
        {
            new MusicTrackDef(AudioIds.Music_Excavation, "Música base mientras se excava, con la capa 1 del mapa."),
            new MusicTrackDef(AudioIds.Music_Excavation_Layer2, "Música tras el cambio de mapa (capa 2)."),
            new MusicTrackDef(AudioIds.Music_Restoration, "Durante los minijuegos de restauración."),
            new MusicTrackDef(AudioIds.Music_Museum, "Con el panel del Museo abierto."),
            new MusicTrackDef(AudioIds.Music_Shop, "Con el panel de la Tienda abierto."),
        };
    }
}