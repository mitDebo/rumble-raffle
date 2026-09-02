namespace RumbleRaffle.Core.Storage;

// App-owned abstraction over wherever images actually live (Supabase
// Storage today) so callers -- attendee profile pictures, wrestler images
// (5.2) -- depend on this, not on Supabase directly. Deliberately minimal:
// the caller decides the path/key (e.g. "wrestlers/42.png"); this interface
// only knows how to put bytes at that path and how to turn a path into a
// URL a browser can load.
public interface IImageStorage
{
    Task UploadAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default);

    string GetUrl(string path);
}
