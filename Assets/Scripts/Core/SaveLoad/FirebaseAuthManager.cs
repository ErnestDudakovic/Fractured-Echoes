// ============================================================================
// FirebaseAuthManager.cs — Anonymous authentication for Firebase
// Signs the player in anonymously so they get a unique UserID.
// This UserID is used as the Firestore document path for their saves.
// No account creation, no email, no password — fully invisible to player.
// ============================================================================

using System;
using UnityEngine;
using Firebase.Auth;

namespace FracturedEchoes.Core.SaveLoad
{
    /// <summary>
    /// Handles anonymous Firebase authentication. Provides a stable UserID
    /// that persists across sessions (until the app is uninstalled).
    /// </summary>
    public class FirebaseAuthManager : MonoBehaviour
    {
        // =====================================================================
        // SINGLETON
        // =====================================================================

        public static FirebaseAuthManager Instance { get; private set; }

        // =====================================================================
        // STATE
        // =====================================================================

        private FirebaseAuth _auth;
        private FirebaseUser _user;
        private bool _isSignedIn;

        /// <summary>True once the player is authenticated.</summary>
        public bool IsSignedIn => _isSignedIn;

        /// <summary>The unique user ID (used as Firestore document key).</summary>
        public string UserID => _user?.UserId ?? string.Empty;

        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>Raised when sign-in succeeds. Passes the UserID.</summary>
        public static event Action<string> OnSignedIn;

        /// <summary>Raised when sign-in fails.</summary>
        public static event Action<string> OnSignInFailed;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            // Wait for Firebase to be ready before signing in
            FirebaseInitializer.OnFirebaseReady += HandleFirebaseReady;

            // If Firebase is already ready (late subscriber)
            if (FirebaseInitializer.Instance != null && FirebaseInitializer.Instance.IsReady)
                HandleFirebaseReady();
        }

        private void OnDisable()
        {
            FirebaseInitializer.OnFirebaseReady -= HandleFirebaseReady;
        }

        // =====================================================================
        // AUTHENTICATION
        // =====================================================================

        private void HandleFirebaseReady()
        {
            _auth = FirebaseAuth.DefaultInstance;

            // Check if already signed in from a previous session
            if (_auth.CurrentUser != null)
            {
                _user = _auth.CurrentUser;
                _isSignedIn = true;
                Debug.Log($"[FirebaseAuth] Already signed in. UserID: {_user.UserId}");
                OnSignedIn?.Invoke(_user.UserId);
                return;
            }

            // Sign in anonymously
            SignInAnonymously();
        }

        private void SignInAnonymously()
        {
            Debug.Log("[FirebaseAuth] Signing in anonymously...");

            _auth.SignInAnonymouslyAsync().ContinueWith(task =>
            {
                // Must dispatch to main thread for Unity API calls
                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    if (task.IsCanceled)
                    {
                        Debug.LogWarning("[FirebaseAuth] Sign-in was cancelled.");
                        OnSignInFailed?.Invoke("Sign-in cancelled.");
                        return;
                    }

                    if (task.IsFaulted)
                    {
                        string error = task.Exception?.Flatten().InnerException?.Message ?? "Unknown error";
                        Debug.LogError($"[FirebaseAuth] Sign-in failed: {error}");
                        OnSignInFailed?.Invoke(error);
                        return;
                    }

                    AuthResult result = task.Result;
                    _user = result.User;
                    _isSignedIn = true;

                    Debug.Log($"[FirebaseAuth] Signed in anonymously. UserID: {_user.UserId}");
                    OnSignedIn?.Invoke(_user.UserId);
                });
            });
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    // =========================================================================
    // HELPER: Main thread dispatcher for Firebase callbacks
    // Firebase callbacks run on background threads — Unity API requires main thread.
    // =========================================================================

    /// <summary>
    /// Dispatches actions to the Unity main thread. Firebase async callbacks
    /// run on background threads and cannot call Unity APIs directly.
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private static readonly System.Collections.Generic.Queue<Action> _queue =
            new System.Collections.Generic.Queue<Action>();

        public static void Enqueue(Action action)
        {
            if (action == null) return;

            // If no instance exists yet, create one
            if (_instance == null)
            {
                var go = new GameObject("[MainThreadDispatcher]");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }

            lock (_queue)
            {
                _queue.Enqueue(action);
            }
        }

        private void Update()
        {
            lock (_queue)
            {
                while (_queue.Count > 0)
                {
                    _queue.Dequeue()?.Invoke();
                }
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
