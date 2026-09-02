using RumbleRaffle.Core.Storage;

namespace RumbleRaffle.Api.UnitTests.Scaffolding;

// In-memory stand-in for SupabaseImageStorage, for unit-testing anything
// that depends on IImageStorage without a real network call. GetUrl
// returns a deterministic, obviously-fake URL so a test can assert on it
// without needing to know anything about Supabase's real URL shape.
public class FakeImageStorage : IImageStorage
{
    private readonly Dictionary<string, byte[]> _uploads = new();

    public async Task UploadAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _uploads[path] = buffer.ToArray();
    }

    public string GetUrl(string path) => $"fake://images/{path}";

    // Lets a test assert on what was actually uploaded, without needing a
    // real storage backend to read anything back from.
    public bool WasUploaded(string path) => _uploads.ContainsKey(path);

    public byte[] GetUploadedContent(string path) => _uploads[path];
}
