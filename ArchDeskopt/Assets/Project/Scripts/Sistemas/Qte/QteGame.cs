using System;
using UnityEngine;

namespace Archaeo.Qte
{
    public enum QteResult { Fail, Good, Perfect }

    /// Parámetros con los que se lanza un QTE (dificultad, habilidades...).
    public class QteContext
    {
        public int RarityIndex;          // 0 Común ... 4 Legendario
        public int ZoneIndex;            // 0..5
        public float Difficulty01;       // 0 fácil ... 1 difícil (según la rareza)
        public bool Easy;                // versión fácil (Instinto / Segunda oportunidad)
        public float WindowScale = 1f;   // Pulso firme: 1 + 0,1 por nivel
        public float ExtraSeconds;       // Reloj de arena: +0,5 s por nivel
        public System.Random Rng = new System.Random();
    }

    /// Base de todos los QTE. No es un MonoBehaviour: construye su propia interfaz dentro de Root
    /// (un rectángulo de unos 300x100 que le da QtePanelView) y recibe los gestos del ratón.
    /// Para crear un QTE nuevo: heredar, rellenar Name/Hint/ComputeDuration/Setup y llamar a Finish(...).
    public abstract class QteGame
    {
        public abstract string Name { get; }
        public abstract string Hint { get; }

        public float Duration { get; private set; }
        public float TimeLeft { get; private set; }
        public bool IsFinished { get; private set; }

        public event Action<QteResult> Finished;

        protected QteContext Ctx { get; private set; }
        protected RectTransform Root { get; private set; }

        /// Duración base en segundos (3 a 6). Se le suma Ctx.ExtraSeconds.
        protected abstract float ComputeDuration();
        /// Crea los elementos visuales dentro de Root.
        protected abstract void Setup();

        public void Begin(RectTransform root, QteContext ctx)
        {
            Root = root;
            Ctx = ctx;
            IsFinished = false;
            Duration = Mathf.Max(1f, ComputeDuration() + ctx.ExtraSeconds);
            TimeLeft = Duration;
            Setup();
        }

        public void Tick(float dt)
        {
            if (IsFinished) return;
            TimeLeft -= dt;
            OnTick(dt);
            if (!IsFinished && TimeLeft <= 0f) OnTimeUp();
        }

        // ---- Ayudas para los QTE concretos ----
        /// Tamaño del área de juego.
        protected Vector2 Size => Root.rect.size;
        /// Elapsed = segundos jugados.
        protected float Elapsed => Duration - TimeLeft;
        /// Valor entre 'easy' (rareza común) y 'hard' (legendario).
        protected float ByDifficulty(float easy, float hard) => Mathf.Lerp(easy, hard, Ctx.Difficulty01);
        /// Zonas de acierto: más grandes con Pulso firme y con la versión fácil.
        protected float Tolerance => Ctx.WindowScale * (Ctx.Easy ? 1.5f : 1f);
        /// Velocidad: más lenta en la versión fácil.
        protected float SpeedFactor => Ctx.Easy ? 0.7f : 1f;

        protected UnityEngine.UI.Image Box(string name, Color color, Vector2 size, Vector2 pos)
            => QteUi.Box(Root, name, color, size, pos);

        protected virtual void OnTick(float dt) { }
        protected virtual void OnTimeUp() => Finish(QteResult.Fail);

        // Gestos (posición local respecto a Root, con el centro en 0,0)
        public virtual void PointerDown(Vector2 local) { }
        public virtual void PointerUp(Vector2 local) { }
        public virtual void PointerMove(Vector2 local) { }
        public virtual void Scroll(float delta) { }

        protected void Finish(QteResult result)
        {
            if (IsFinished) return;
            IsFinished = true;
            Finished?.Invoke(result);
        }

        /// Borra la interfaz del QTE.
        public void Cleanup()
        {
            if (Root == null) return;
            for (int i = Root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(Root.GetChild(i).gameObject);
        }
    }
}
