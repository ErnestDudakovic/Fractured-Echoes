// ============================================================================
// SceneLoader.cs — Async scene loading with loading screen support
// Handles scene transitions with a fade overlay and progress bar.
// Lives on a DontDestroyOnLoad GameObject so it persists across scenes.
// ============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FracturedEchoes.Core.Events;

namespace FracturedEchoes.UI
{
    /// <summary>
    /// Manages async scene loading with a configurable loading screen.
    /// Place on a root GameObject with a Canvas child containing the
    /// loading UI elements.  Marks itself DontDestroyOnLoad.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        // =====================================================================
        // SINGLETON (lightweight — only for cross-scene persistence)
        // =====================================================================

        public static SceneLoader Instance { get; private set; }

        // =====================================================================
        // SERIALIZED FIELDS
        // =====================================================================

        [Header("Loading Screen UI")]
        [Tooltip("Root canvas / panel of the loading screen.")]
        [SerializeField] private CanvasGroup _loadingScreenGroup;

        [Tooltip("Fill image used as a progress bar (Image.fillAmount).")]
        [SerializeField] private UnityEngine.UI.Image _progressBar;

        [Tooltip("Optional text showing the loading percentage.")]
        [SerializeField] private TMPro.TextMeshProUGUI _progressText;

        [Tooltip("Optional text showing a loading tip.")]
        [SerializeField] private TMPro.TextMeshProUGUI _tipText;

        [Header("Fade Settings")]
        [Tooltip("Duration of the fade-in / fade-out of the loading screen.")]
        [SerializeField] private float _fadeDuration = 0.5f;

        [Tooltip("Minimum time the loading screen stays visible (seconds). " +
                 "Prevents jarring flash on fast loads.")]
        [SerializeField] private float _minimumLoadTime = 1.5f;

        [Header("Tips")]
        [Tooltip("Random tips / lore shown during loading.")]
        [SerializeField, TextArea(1, 3)] private string[] _tips;

        [Header("Events")]
        [SerializeField] private GameEvent _onLoadStarted;
        [SerializeField] private GameEvent _onLoadFinished;

        // =====================================================================
        // RUNTIME
        // =====================================================================

        private bool _isLoading;
        public bool IsLoading => _isLoading;

        /// <summary>Fires when a load finishes, passing the new scene name.</summary>
        public event Action<string> SceneLoaded;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            // Singleton guard
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Start hidden
            if (_loadingScreenGroup != null)
            {
                _loadingScreenGroup.alpha = 0f;
                _loadingScreenGroup.gameObject.SetActive(false);
            }
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Load a scene by name with a loading screen transition.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (_isLoading) return;
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        /// <summary>
        /// Load a scene by build index.
        /// </summary>
        public void LoadScene(int buildIndex)
        {
            if (_isLoading) return;
            string sceneName = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        /// <summary>
        /// Reload the currently active scene.
        /// </summary>
        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Load the main menu scene (assumed build index 0 or name "MainMenu").
        /// </summary>
        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            LoadScene("MainMenu");
        }

        // =====================================================================
        // LOADING COROUTINE
        // =====================================================================

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            _isLoading = true;
            _onLoadStarted?.Raise();

            // Pick a random tip
            if (_tipText != null && _tips != null && _tips.Length > 0)
            {
                _tipText.text = _tips[UnityEngine.Random.Range(0, _tips.Length)];
            }

            // Reset progress
            SetProgress(0f);

            // Fade in loading screen
            yield return FadeLoadingScreen(1f);

            // Start async load
            float loadStart = Time.unscaledTime;
            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            // Update progress bar while loading
            while (!asyncOp.isDone)
            {
                // Unity reports 0 → 0.9 while loading, then waits for activation
                float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);
                SetProgress(progress);

                // When loading is done (0.9), check minimum display time
                if (asyncOp.progress >= 0.9f)
                {
                    float elapsed = Time.unscaledTime - loadStart;
                    if (elapsed >= _minimumLoadTime)
                    {
                        SetProgress(1f);
                        yield return null; // one frame at 100%
                        asyncOp.allowSceneActivation = true;
                    }
                }

                yield return null;
            }

            // Small delay for scene initialization
            yield return new WaitForSecondsRealtime(0.2f);

            // Fade out loading screen
            yield return FadeLoadingScreen(0f);

            _isLoading = false;
            _onLoadFinished?.Raise();
            SceneLoaded?.Invoke(sceneName);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private void SetProgress(float value)
        {
            if (_progressBar != null)
            {
                _progressBar.fillAmount = value;
            }

            if (_progressText != null)
            {
                _progressText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        private IEnumerator FadeLoadingScreen(float targetAlpha)
        {
            if (_loadingScreenGroup == null) yield break;

            _loadingScreenGroup.gameObject.SetActive(true);
            float startAlpha = _loadingScreenGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _loadingScreenGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _fadeDuration);
                yield return null;
            }

            _loadingScreenGroup.alpha = targetAlpha;

            // Deactivate when fully transparent
            if (targetAlpha <= 0f)
            {
                _loadingScreenGroup.gameObject.SetActive(false);
            }
        }
    }
}
