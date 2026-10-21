using System;
using System.Collections.Generic;

namespace Archaeo.Save
{
    /// Estado de un artefacto en el guardado.
    [Serializable]
    public class ArtifactSave
    {
        public string id;
        public int level;
        public bool restored;
    }

    /// Todo lo que se guarda de la partida. Se serializa a JSON.
    /// Si cambia su estructura, sube "version" y añade una migración en SaveManager.
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public long savedAtUnix;               // segundos UTC, para los ingresos con el juego cerrado

        public double coins;

        public float excavationPoints;
        public int totalFinds;

        public List<ArtifactSave> artifacts = new List<ArtifactSave>();
        public List<string> pending = new List<string>();   // ids pendientes de restaurar, en orden
    }
}