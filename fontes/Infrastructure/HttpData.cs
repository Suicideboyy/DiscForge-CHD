using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

static class HttpData
{
    static readonly HttpClient Client = new(new HttpClientHandler { MaxAutomaticRedirections = 4 });

    public static async Task<byte[]> DownloadAsync(string url, int limit)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(AppInfo.UserAgent);
        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
            timeout.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > limit)
            throw new IOException("Download maior que o limite permitido.");
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        using var memory = new MemoryStream();
        byte[] buffer = new byte[81920];
        int count;
        while ((count = await stream.ReadAsync(buffer, timeout.Token).ConfigureAwait(false)) > 0)
        {
            if (memory.Length + count > limit)
                throw new IOException("Download maior que o limite permitido.");
            memory.Write(buffer, 0, count);
        }
        return memory.ToArray();
    }
}
