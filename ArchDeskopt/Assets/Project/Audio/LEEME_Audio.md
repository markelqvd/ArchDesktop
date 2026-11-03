# Guía de audio — Archaeo

Esta guía es para quien hace el sonido. **No hace falta saber programar ni saber Unity.**

**El juego no lleva música**: solo efectos, sonidos de la interfaz y, de forma opcional, sonido ambiente.

## Cómo funciona, en una frase

El juego avisa de lo que pasa ("sale un objeto nuevo", "se hace clic en un botón") y cada aviso tiene un **evento de sonido**. Tú decides qué suena en cada evento. No hay que tocar código.

## Dónde va cada cosa

Todo está dentro de `Assets/Project/Audio/`:

| Carpeta | Qué lleva |
|---|---|
| `UI/` | Sonidos de la interfaz (botones, paneles, compras) |
| `SFX/` | Efectos del juego (hallazgos, minijuegos, ventana...) |
| `Ambiente/` | Sonidos ambiente en bucle (viento, hoguera) |
| `Eventos/` | Los "eventos" (no hace falta tocarlos, ver más abajo) |

## Cómo añadir un sonido (lo fácil)

1. Exporta el sonido como **WAV** y ponle **el nombre del evento**, seguido de un número si hay varias versiones:
   `UI_Click_01.wav`, `UI_Click_02.wav`, `UI_Click_03.wav`
2. Copia los archivos a la carpeta que toque (por ejemplo `Audio/UI/`).
3. Abre Unity y espera unos segundos: **se asignan solos**. Ya está.

Si hay varias versiones del mismo evento, el juego elige una al azar sin repetir la anterior.

## Probar lo que has hecho

En `Audio/Eventos/` hay un archivo por evento. Haz clic en uno y verás sus ajustes con un botón **▶ Probar**. Si no suena, activa el icono del altavoz de la barra de la vista *Scene*.

Ahí puedes ajustar, si quieres:

- **Volume Db**: volumen del evento en decibelios (0 = sin cambios, −6 = la mitad, −12 = un cuarto).
- **Volume Variation Db**: cuánto varía el volumen al azar en cada reproducción (0 a 2 queda natural).
- **Pitch Semitones** y **Pitch Variation Semitones**: tono fijo y variación al azar, en semitonos.
- **Cooldown Seconds**: tiempo mínimo entre dos reproducciones (evita ráfagas).
- **Max Instances**: cuántas copias pueden sonar a la vez.
- **Play In Background**: si suena cuando el jugador está en otro programa. Los sonidos que se repiten mucho (clics, teclas) están desactivados para no molestar.

Todo lo que no toques se queda con valores razonables ya puestos.

## Especificaciones técnicas

- **Formato:** WAV, 44,1 o 48 kHz, 16 o 24 bits. No MP3 (mete silencio al inicio y rompe los bucles).
- **Efectos y interfaz:** picos máximos a **−3 dBFS**, sin recortes, sin silencio al principio ni al final.
- **Bucles** (ambiente y sonidos continuos): cortados exactamente en el punto de unión, sin fundidos en los extremos y con la misma longitud en ambos canales.
- **Los sonidos que se repiten mucho** (`Activity_*`, `UI_Hover`, `Effect_*`) tienen que ser muy cortos y suaves.
- Cada archivo, mejor sin el nombre del evento duplicado en otras carpetas.

## Lista de eventos

| Evento (nombre del archivo) | Carpeta | Bucle | Cuándo suena |
|---|---|---|---|
| `UI_Click` | UI | no | Clic en cualquier botón de la interfaz. |
| `UI_Hover` | UI | no | El ratón pasa por encima de un botón. Muy suave. |
| `UI_PanelOpen` | UI | no | Se abre un panel (Museo, Tienda, Localizaciones o Ajustes). |
| `UI_PanelClose` | UI | no | Se cierra un panel. |
| `UI_Toggle` | UI | no | Se marca o desmarca una casilla en Ajustes. |
| `UI_Toast` | UI | no | Aparece un aviso pequeño de texto en la ventana. |
| `UI_BuyUpgrade` | UI | no | Se compra una mejora en la tienda. |
| `UI_BuyDenied` | UI | no | Se intenta comprar una mejora sin monedas suficientes. |
| `Activity_Click` | SFX | no | El jugador hace un clic, en cualquier programa. Suena muy a menudo: debe ser corto y muy suave. |
| `Activity_Key` | SFX | no | El jugador pulsa una tecla, en cualquier programa. Suena muy a menudo: debe ser corto y muy suave. |
| `Excavation_Find` | SFX | no | La barra de excavación se llena: ha aparecido algo (todavía no se sabe qué). |
| `Excavation_EarlyFind` | SFX | no | Hallazgo por suerte, antes de llenar la barra. |
| `Artifact_New` | SFX | no | Sale un objeto NUEVO. Es el sonido general: se usa si el de su rareza está vacío. |
| `Artifact_New_Common` | SFX | no | Sale un objeto nuevo de rareza Común. Si no tiene sonido, suena Artifact_New. |
| `Artifact_New_Uncommon` | SFX | no | Sale un objeto nuevo de rareza Poco común. Si no tiene sonido, suena Artifact_New. |
| `Artifact_New_Rare` | SFX | no | Sale un objeto nuevo de rareza Raro. Si no tiene sonido, suena Artifact_New. |
| `Artifact_New_VeryRare` | SFX | no | Sale un objeto nuevo de rareza Muy raro. Si no tiene sonido, suena Artifact_New. |
| `Artifact_New_Relic` | SFX | no | Sale un objeto nuevo de rareza Reliquia. Si no tiene sonido, suena Artifact_New. |
| `Artifact_Duplicate` | SFX | no | Sale un objeto que ya tenías y sube de nivel. Discreto: no debe interrumpir. |
| `Artifact_Restored` | SFX | no | Se completa una restauración y el objeto pasa al museo. |
| `Clean_Start` | SFX | no | Se abre el minijuego de limpiar. |
| `Clean_Scrub` | SFX | sí | Mientras el jugador limpia con el ratón. Es un bucle: suena solo mientras frota. |
| `Clean_Complete` | SFX | no | Se termina de limpiar el objeto. |
| `Clean_Fail` | SFX | no | Se acaba el tiempo del minijuego sin completarlo. |
| `Minigame_TimerWarning` | SFX | no | Quedan 5 segundos o menos en un minijuego (un tic por segundo). |
| `Examine_Start` | SFX | no | Se abre el minijuego de examinar. |
| `Examine_PointHit` | SFX | no | El jugador acierta uno de los puntos. Conviene tener varias variaciones. |
| `Examine_Complete` | SFX | no | Se aciertan todos los puntos del minijuego de examinar. |
| `Examine_Fail` | SFX | no | Se acaba el tiempo del minijuego de examinar sin completarlo. |
| `Map_Transition` | SFX | no | El mapa cambia de capa con la nube de polvo. |
| `Effect_PickHit` | SFX | no | Un picazo de la animación del pico sobre el mapa. |
| `Effect_BrushSweep` | SFX | no | Un barrido de la animación de la brocha sobre el mapa. |
| `Ambient_Zone1` | Ambiente | sí | Ambiente de la zona 1 (viento, pájaros...). Bucle continuo. |
| `Ambient_Campfire` | Ambiente | sí | Crepitar de la hoguera. Solo suena con la capa 2 del mapa. Bucle. |
| `Economy_OfflineIncome` | SFX | no | Al abrir el juego: el museo ha generado monedas mientras estabas fuera. |
| `Window_Start` | SFX | no | El juego se abre. |
| `Window_Minimize` | SFX | no | Se minimiza la ventana. |
| `Window_Snap` | SFX | no | La ventana se pega a un borde de la pantalla. |
| `Window_Quit` | SFX | no | Se cierra el juego. |

## Cómo elige la música el juego

El juego cambia de pista solo, con este orden de prioridad (gana la primera que tenga música):

| Cuándo | Pista |
|---|---|
| Hay un minijuego de restauración en marcha | `Music_Restoration` |
| El panel del Museo está abierto | `Music_Museum` |
| El panel de la Tienda está abierto | `Music_Shop` |
| Se está viendo la capa 2 del mapa (tras el polvo) | `Music_Excavation_Layer2` |
| El resto del tiempo | `Music_Excavation` |

**Si una pista todavía no existe, no pasa nada**: el juego usa la siguiente de la lista, así que nunca se queda sin música. Al cambiar, la nueva entra con un fundido (por defecto 2 segundos; se cambia en la pista con *Crossfade Seconds*).

## Ambiente

- `Ambient_Zone1`: suena siempre, en bucle.
- `Ambient_Campfire`: solo cuando el mapa está en la capa de la hoguera (la 2). En bucle.

Los dos callan cuando el jugador está en otro programa, y vuelven al regresar.

## Panel de pruebas

En Unity, menú **Archaeo > Audio > Panel de pruebas**. Muestra todos los eventos y todas las pistas:

- **✔ / ✗**: si ya tienen sonido o les falta.
- **▶**: para escuchar cada uno con sus ajustes (volumen, tono y variaciones incluidos).
- Un buscador y el filtro "Solo los que faltan", para ver qué queda por entregar.
- Pulsar el nombre de un evento abre su archivo, para ajustarlo.

## Preguntas frecuentes

**He añadido un sonido y no suena en el juego.** Comprueba que el nombre del archivo empieza *exactamente* por el nombre del evento (mayúsculas incluidas) y que está dentro de `Assets/Project/Audio/`. Después, en Unity, menú **Archaeo > Audio > 3. Informe de cobertura**: te dice qué eventos están todavía sin sonido.

**Quiero cambiar un sonido.** Sustituye el archivo por otro con el mismo nombre.

**Necesito un evento que no está en la lista.** Pídeselo al equipo de programación: se añade en un minuto.

**¿Cómo entrego el trabajo?** Copia los archivos en la carpeta indicada y súbelos con Git, o pásaselos al equipo.
