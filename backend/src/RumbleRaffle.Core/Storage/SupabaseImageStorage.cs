using System.Net.Http.Headers;

namespace RumbleRaffle.Core.Storage;

// Talks to Supabase Storage's plain REST API directly rather than pulling
// in a third-party SDK -- there's no official Supabase .NET package, and
// two operations (upload, build a URL) don't justify adopting one.
// Configured as a typed HttpClient (see
// ServiceCollectionExtensions.AddRumbleRaffleCore) whose base address and
// auth header get set from IConfiguration lazily, at the point the client
// is actually created -- same reasoning as ConnectionStrings.Resolve being
// called inside AddDbContext's callback rather than eagerly at startup.
public class SupabaseImageStorage : IImageStorage
{
    // One bucket is enough for MVP; revisit if a second one is ever needed.
    private const string Bucket = "images";

    private readonly HttpClient _httpClient;

    public SupabaseImageStorage(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task UploadAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        // StreamContent takes ownership of `content` and disposes it when
        // the request completes -- callers should treat the stream as
        // consumed after this returns, the same as handing it to any other
        // HttpContent.
        using var requestContent = new StreamContent(content);
        requestContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        // x-upsert overwrites rather than failing if something's already at
        // this path -- simpler for callers than making them delete first
        // (re-uploading a wrestler's image just replaces it).
        using var request = new HttpRequestMessage(HttpMethod.Post, $"storage/v1/object/{Bucket}/{path}")
        {
            Content = requestContent,
        };
        request.Headers.Add("x-upsert", "true");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public string GetUrl(string path)
    {
        // No network call -- the bucket is public, so the URL shape is
        // deterministic. A private bucket would need this to become an
        // async signed-URL request instead of plain string-building.
        return new Uri(_httpClient.BaseAddress!, $"storage/v1/object/public/{Bucket}/{path}").ToString();
    }
}
