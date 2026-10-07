using UnityEngine;
using UnityEngine.SceneManagement;


namespace Archaeo.Core
{
    /// Punto de entrada. Vive en la escena Boot y persiste entre escenas.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameStateMachine StateMachine { get; } = new GameStateMachine();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.runInBackground = true; // imprescindible: el juego progresa sin foco
            StateMachine.OnStateChanged += (a, b) => Debug.Log($"[State] {a} -> {b}");
            if (SceneManager.GetActiveScene().name == "Boot")
                SceneManager.LoadScene("Main");
        }

#if UNITY_EDITOR
        // Atajos de prueba para el día 1
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) StateMachine.TryChangeState(GameState.Excavating);
            if (Input.GetKeyDown(KeyCode.Alpha2)) StateMachine.TryChangeState(GameState.Discovery);
            if (Input.GetKeyDown(KeyCode.Alpha3)) StateMachine.TryChangeState(GameState.Restoration);
            if (Input.GetKeyDown(KeyCode.Alpha4)) StateMachine.TryChangeState(GameState.Museum);
        }
#endif
    }
}