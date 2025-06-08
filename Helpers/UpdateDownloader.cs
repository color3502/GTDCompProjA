using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GTDCompanion.Helpers
{
    public static class UpdateDownloader
    {
        public static async Task<string> DownloadAsync(string url, IProgress<double>? progress = null)
        {
            var target = Path.Combine(Path.GetTempPath(), "GTDCompanion_Installer.exe");
            using var client = new HttpClient();
            using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? -1L;
            await using var stream = await resp.Content.ReadAsStreamAsync();
            await using var fs = File.Create(target);
            var buffer = new byte[81920];
            long read;
            long totalRead = 0;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, (int)read));
                totalRead += read;
                if (total > 0 && progress != null)
                {
                    double pct = (double)totalRead / total * 100;
                    progress.Report(pct);
                }
            }
            progress?.Report(100);
            return target;
        }
    }
}
