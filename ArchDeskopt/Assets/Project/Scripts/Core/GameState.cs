namespace Archaeo.Core
{
    public enum GameState
    {
        Excavating,   // Excavación pasiva (estado por defecto)
        Discovery,    // Hallazgo generado, esperando a que el jugador lo atienda
        Restoration,  // QTE de restauración (solo objetos nuevos)
        Museum        // Vista del museo / mejoras
    }
}