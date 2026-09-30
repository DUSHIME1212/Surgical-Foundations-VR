using System;
using System.Threading;
using System.Threading.Tasks;
using SurgicalFoundations.Contracts;
using UnityEngine;

namespace SurgicalFoundations.Backend
{
    public enum AuthState { SignedOut, RequestingCode, WaitingForPhone, SigningIn, SignedIn }

    /// <summary>The signed-in account, kept on the headset so it can train and queue results offline for days (NFR-09).</summary>
    [Serializable]
    public class StoredAccount
    {
        public string refreshToken;
        public string userId;
        public string displayName;
        public UserRole role;
        public string launchId;
    }

    /// <summary>
    /// Headset sign-in (FR-01): device flow (QR / user code approved on a phone), or the session code from an LMS launch.
    /// Only the refresh token is persisted; access tokens stay in memory and are renewed on demand.
    /// </summary>
    public class AuthService
    {
        // PlayerPrefs is app-private storage on Quest (Android SharedPreferences, on an encrypted device).
        // Upgrade path: Android Keystore, if a customer's security review asks for hardware-backed storage.
        const string AccountKey = "sf.account.v1";

        readonly ApiClient api;
        StoredAccount account;
        string accessToken;
        DateTime accessTokenExpiresUtc;
        Task<string> refreshing;
        CancellationTokenSource deviceFlow;

        public AuthService(ApiClient api)
        {
            this.api = api;
            account = LoadAccount();
            State = account != null ? AuthState.SignedIn : AuthState.SignedOut;
        }

        public event Action Changed;

        public AuthState State { get; private set; }
        public DeviceCodeResponse PendingCode { get; private set; }
        public DateTime PendingCodeExpiresUtc { get; private set; }
        public string LastError { get; private set; }

        /// <summary>True once someone has signed in on this headset, even while offline now.</summary>
        public bool HasAccount => account != null;
        public string UserId => account?.userId;
        public string DisplayName => account?.displayName;
        public UserRole Role => account?.role ?? UserRole.Learner;
        /// <summary>Signed in through an LMS launch: Assessment results also go back to that LMS.</summary>
        public bool FromLmsLaunch => !string.IsNullOrEmpty(account?.launchId);

        /// <summary>Training without an account: nothing is recorded or uploaded.</summary>
        public bool IsGuest { get; private set; }

        public void ContinueAsGuest()
        {
            CancelDeviceSignIn();
            IsGuest = true;
            Raise();
        }

        public void LeaveGuestMode()
        {
            IsGuest = false;
            Raise();
        }

        // ───────────── Device flow ─────────────

        /// <summary>Requests a code and polls until the learner approves it on their phone. Expired codes are replaced automatically.</summary>
        public async void StartDeviceSignIn(CancellationToken lifetime)
        {
            CancelDeviceSignIn();
            deviceFlow = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
            var ct = deviceFlow.Token;
            try { await RunDeviceFlowAsync(ct); }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
        }

        public void CancelDeviceSignIn()
        {
            deviceFlow?.Cancel();
            deviceFlow?.Dispose();
            deviceFlow = null;
            PendingCode = null;
            if (State == AuthState.RequestingCode || State == AuthState.WaitingForPhone) State = HasAccount ? AuthState.SignedIn : AuthState.SignedOut;
        }

        async Task RunDeviceFlowAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && !HasAccount)
            {
                SetState(AuthState.RequestingCode);
                var response = await api.PostAsync(ApiRoutes.DeviceCode,
                    new DeviceCodeRequest { deviceId = SystemInfo.deviceUniqueIdentifier, deviceName = DeviceName() }, null, ct);
                if (!response.Ok)
                {
                    LastError = response.Outcome == ApiOutcome.Offline ? "Offline — sign-in needs a connection." : "Can't reach the server right now.";
                    SetState(AuthState.SignedOut);
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    continue;
                }

                LastError = null;
                PendingCode = response.Read<DeviceCodeResponse>();
                PendingCodeExpiresUtc = DateTime.UtcNow.AddSeconds(PendingCode.expiresIn);
                SetState(AuthState.WaitingForPhone);

                var interval = Math.Max(1, PendingCode.interval);
                while (!ct.IsCancellationRequested && DateTime.UtcNow < PendingCodeExpiresUtc)
                {
                    await Task.Delay(TimeSpan.FromSeconds(interval), ct);
                    var poll = await api.PostAsync(ApiRoutes.Token, new TokenRequest
                    {
                        grantType = GrantTypes.DeviceCode,
                        deviceCode = PendingCode.deviceCode,
                        deviceId = SystemInfo.deviceUniqueIdentifier
                    }, null, ct);

                    if (poll.Ok) { Accept(poll.Read<TokenResponse>()); return; }
                    if (poll.Outcome != ApiOutcome.Rejected) continue; // offline blip: keep polling

                    var error = poll.Read<TokenError>()?.error;
                    if (error == TokenErrors.AuthorizationPending) continue;
                    if (error == TokenErrors.SlowDown) { interval += 5; continue; }
                    if (error == TokenErrors.AccessDenied) LastError = "Sign-in was declined on the phone.";
                    break; // expired, denied or unknown: get a fresh code
                }
            }
        }

        // ───────────── LMS launch code ─────────────

        public async Task<(bool ok, string message)> SignInWithLaunchCodeAsync(string code, CancellationToken ct)
        {
            SetState(AuthState.SigningIn);
            var response = await api.PostAsync(ApiRoutes.Token, new TokenRequest
            {
                grantType = GrantTypes.LaunchCode,
                launchCode = code,
                deviceId = SystemInfo.deviceUniqueIdentifier
            }, null, ct);

            if (response.Ok)
            {
                CancelDeviceSignIn();
                Accept(response.Read<TokenResponse>());
                return (true, null);
            }
            SetState(HasAccount ? AuthState.SignedIn : AuthState.SignedOut);
            if (response.Outcome == ApiOutcome.Offline) return (false, "Offline — connect to Wi-Fi to use a session code.");
            if (response.Outcome == ApiOutcome.Rejected)
                return (false, response.Read<TokenError>()?.errorDescription ?? "That code wasn't accepted.");
            return (false, "The server is busy. Try again in a moment.");
        }

        // ───────────── Tokens ─────────────

        /// <summary>A valid access token, refreshing if needed. Null when offline or signed out.</summary>
        public Task<string> GetAccessTokenAsync(CancellationToken ct)
        {
            if (account == null) return Task.FromResult<string>(null);
            if (accessToken != null && DateTime.UtcNow < accessTokenExpiresUtc) return Task.FromResult(accessToken);
            // One refresh at a time: refresh tokens rotate, so two parallel refreshes would revoke the session.
            return refreshing ??= RefreshAsync(ct);
        }

        public void InvalidateAccessToken() => accessToken = null;

        async Task<string> RefreshAsync(CancellationToken ct)
        {
            try
            {
                var response = await api.PostAsync(ApiRoutes.Token,
                    new TokenRequest { grantType = GrantTypes.RefreshToken, refreshToken = account.refreshToken }, null, ct);
                if (response.Ok)
                {
                    Accept(response.Read<TokenResponse>());
                    return accessToken;
                }
                if (response.Outcome == ApiOutcome.Rejected)
                {
                    // Revoked, expired or the account was disabled: the learner must sign in again.
                    // Queued results for this user stay on disk and upload after they do.
                    LastError = "Your sign-in has expired. Please sign in again.";
                    ClearAccount();
                }
                return null;
            }
            finally { refreshing = null; }
        }

        public async void SignOut()
        {
            var token = account?.refreshToken;
            ClearAccount();
            IsGuest = false;
            if (token == null) return;
            try { await api.PostAsync(ApiRoutes.Revoke, new RevokeBody { token = token }, null, CancellationToken.None); }
            catch (Exception) { /* best effort: the token also expires on its own */ }
        }

        [Serializable] class RevokeBody { public string token; }

        void Accept(TokenResponse tokens)
        {
            account = new StoredAccount
            {
                refreshToken = tokens.refreshToken,
                userId = tokens.user?.id ?? account?.userId,
                displayName = tokens.user?.displayName ?? account?.displayName,
                role = tokens.user?.role ?? UserRole.Learner,
                launchId = tokens.launchId
            };
            SaveAccount(account);
            accessToken = tokens.accessToken;
            accessTokenExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, tokens.expiresIn - 60));
            PendingCode = null;
            IsGuest = false;
            LastError = null;
            SetState(AuthState.SignedIn);
        }

        void ClearAccount()
        {
            account = null;
            accessToken = null;
            PlayerPrefs.DeleteKey(AccountKey);
            PlayerPrefs.Save();
            SetState(AuthState.SignedOut);
        }

        void SetState(AuthState state)
        {
            State = state;
            Raise();
        }

        void Raise() => Changed?.Invoke();

        static StoredAccount LoadAccount()
        {
            var json = PlayerPrefs.GetString(AccountKey, null);
            if (string.IsNullOrEmpty(json)) return null;
            try { return ApiJson.Deserialize<StoredAccount>(json); }
            catch (Exception) { return null; }
        }

        static void SaveAccount(StoredAccount a)
        {
            PlayerPrefs.SetString(AccountKey, ApiJson.Serialize(a));
            PlayerPrefs.Save();
        }

        static string DeviceName() =>
            string.IsNullOrEmpty(SystemInfo.deviceName) || SystemInfo.deviceName == "<unknown>" ? SystemInfo.deviceModel : SystemInfo.deviceName;
    }
}
