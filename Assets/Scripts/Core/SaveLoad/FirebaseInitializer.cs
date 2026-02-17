// ============================================================================
// FirebaseInitializer.cs — Firebase SDK initialization
// Must run BEFORE any other Firebase script. Initializes the SDK and
// raises a C# event when ready. Other Firebase scripts wait for this.
// Attach to a persistent GameObject (e.g. CoreSystems).
// ============================================================================

using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Extensions;

namespace FracturedEchoes.Core.SaveLoad
{
    /// <summary>
    /// Initializes Firebase SDK. All other Firebase scripts should wait
    /// for <see cref="IsReady"/> or subscribe to <see cref="OnFirebaseReady"/>.
    /// </summary>
    public class FirebaseInitializer : MonoBehaviour
    {
        // =====================================================================
        // SINGLETON (optional — one Firebase init per game)
        // =====================================================================

        public static FirebaseInitializer Instance { get; private set; }

        // =====================================================================
        // STATE
        // =====================================================================

        private bool _isReady;
        private FirebaseApp _app;

        /// <summary>True once Firebase has been successfully initialized.</summary>
        public bool IsReady => _isReady;

        /// <summary>The Firebase app instance.</summary>
        public FirebaseApp App => _app;

        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Raised when Firebase is ready to use.</summary>
        public static event Action OnFirebaseReady;

        /// <summary>Raised if Firebase initialization fails.</summary>
        public static event Action<string> OnFirebaseFailed;

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

            InitializeFirebase();
        }

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        private void InitializeFirebase()
        {
            Debug.Log("[Firebase] Checking dependencies...");

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;

                if (dependencyStatus == DependencyStatus.Available)
                {
                    _app = FirebaseApp.DefaultInstance;
                    _isReady = true;

                    Debug.Log("[Firebase] Initialized successfully.");
                    OnFirebaseReady?.Invoke();
                }
                else
                {
                    string error = $"Could not resolve Firebase dependencies: {dependencyStatus}";
                    Debug.LogError($"[Firebase] {error}");
                    OnFirebaseFailed?.Invoke(error);
                }
            });
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
