using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.Networking;

namespace SurgicalFoundations.Backend
{
    /// <summary>JSON settings matching the API: contract fields as-is, enums as names ("Deviation").</summary>
    public static class ApiJson
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string Serialize(object value) => JsonConvert.SerializeObject(value, Settings);
        public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);
    }

    /// <summary>
    /// What the caller should do next. The sync queue relies on this split: <see cref="Rejected"/> is dropped
    /// (retrying can never succeed), <see cref="Offline"/> and <see cref="Retry"/> are tried again later.
    /// </summary>
    public enum ApiOutcome { Ok, Offline, Unauthorized, Rejected, Retry }

    public readonly struct ApiResponse
    {
        public readonly ApiOutcome Outcome;
        public readonly long Status;
        public readonly string Body;
        public readonly string Error;

        public ApiResponse(ApiOutcome outcome, long status, string body, string error)
        {
            Outcome = outcome; Status = status; Body = body; Error = error;
        }

        public bool Ok => Outcome == ApiOutcome.Ok;

        public T Read<T>() => string.IsNullOrEmpty(Body) ? default : ApiJson.Deserialize<T>(Body);

        public override string ToString() => $"{Outcome} {Status} {Error} {Body}";
    }

    /// <summary>Thin async wrapper over UnityWebRequest. Continuations run on the main thread (Unity's sync context).</summary>
    public class ApiClient
    {
        readonly BackendSettings settings;

        public ApiClient(BackendSettings settings) => this.settings = settings;

        public string BaseUrl => settings.apiBaseUrl.TrimEnd('/');

        public Task<ApiResponse> GetAsync(string path, string accessToken, CancellationToken ct) =>
            SendJsonAsync("GET", path, null, accessToken, ct);

        public Task<ApiResponse> PostAsync(string path, object body, string accessToken, CancellationToken ct) =>
            SendJsonAsync("POST", path, body == null ? null : ApiJson.Serialize(body), accessToken, ct);

        public async Task<ApiResponse> SendJsonAsync(string method, string path, string json, string accessToken, CancellationToken ct)
        {
            if (!settings.online) return new ApiResponse(ApiOutcome.Offline, 0, null, "Backend disabled in BackendSettings.");
            using var request = new UnityWebRequest(BaseUrl + path, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = settings.requestTimeoutSeconds
            };
            if (json != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)) { contentType = "application/json" };
                request.SetRequestHeader("Content-Type", "application/json");
            }
            request.SetRequestHeader("Accept", "application/json");
            if (!string.IsNullOrEmpty(accessToken)) request.SetRequestHeader("Authorization", "Bearer " + accessToken);

            await SendAsync(request, ct);
            return Classify(request);
        }

        /// <summary>Uploads a file to a pre-signed storage URL (not the API, so no bearer token).</summary>
        public async Task<ApiResponse> PutFileAsync(string url, string filePath, string contentType, CancellationToken ct)
        {
            if (!settings.online) return new ApiResponse(ApiOutcome.Offline, 0, null, "Backend disabled in BackendSettings.");
            using var request = new UnityWebRequest(url, "PUT")
            {
                uploadHandler = new UploadHandlerFile(filePath),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", contentType);
            await SendAsync(request, ct);
            return Classify(request);
        }

        public async Task<Texture2D> GetTextureAsync(string path, CancellationToken ct)
        {
            if (!settings.online) return null;
            using var request = UnityWebRequestTexture.GetTexture(BaseUrl + path);
            request.timeout = settings.requestTimeoutSeconds;
            await SendAsync(request, ct);
            return request.result == UnityWebRequest.Result.Success ? DownloadHandlerTexture.GetContent(request) : null;
        }

        static async Task SendAsync(UnityWebRequest request, CancellationToken ct)
        {
            var done = new TaskCompletionSource<bool>();
            var op = request.SendWebRequest();
            if (op.isDone) done.TrySetResult(true);
            else op.completed += _ => done.TrySetResult(true);
            using (ct.Register(() => { if (!request.isDone) request.Abort(); }))
                await done.Task;
            ct.ThrowIfCancellationRequested();
        }

        static ApiResponse Classify(UnityWebRequest request)
        {
            var status = request.responseCode;
            var body = request.downloadHandler?.text;
            if (request.result == UnityWebRequest.Result.ConnectionError || status == 0)
                return new ApiResponse(ApiOutcome.Offline, status, body, request.error);
            if (status >= 200 && status < 300) return new ApiResponse(ApiOutcome.Ok, status, body, null);
            if (status == 401) return new ApiResponse(ApiOutcome.Unauthorized, status, body, request.error);
            if (status == 408 || status == 429 || status >= 500) return new ApiResponse(ApiOutcome.Retry, status, body, request.error);
            return new ApiResponse(ApiOutcome.Rejected, status, body, request.error);
        }
    }
}
