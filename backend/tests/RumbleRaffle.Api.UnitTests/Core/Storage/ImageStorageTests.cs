using System.Text;
using RumbleRaffle.Api.UnitTests.Scaffolding;
using Xunit;

namespace RumbleRaffle.Api.UnitTests.Core.Storage;

// Proves IImageStorage's contract holds -- upload something, then GetUrl
// for that same path resolves to something usable -- using FakeImageStorage
// as the stand-in, per 1.9's plan. This can't (as a unit test) prove
// SupabaseImageStorage actually talks to Supabase correctly; that's only
// verifiable against a real bucket, deliberately deferred until 5.2 needs
// one (see tasks.md).
public class ImageStorageTests
{
    [Fact]
    public async Task UploadAsync_ThenGetUrl_RoundTripsTheUploadedContent()
    {
        var storage = new FakeImageStorage();
        var content = Encoding.UTF8.GetBytes("fake image bytes");

        await storage.UploadAsync("wrestlers/42.png", new MemoryStream(content), "image/png");

        Assert.True(storage.WasUploaded("wrestlers/42.png"));
        Assert.Equal(content, storage.GetUploadedContent("wrestlers/42.png"));
        Assert.Equal("fake://images/wrestlers/42.png", storage.GetUrl("wrestlers/42.png"));
    }
}
