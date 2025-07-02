using System.IO;
using System.IO.Compression;
using System.Net;

namespace Hypernex.Tools
{
    public static class FFMpegDownloader
    {
        // win64
        // This build is LGPL, see https://ffmpeg.org/legal.html
        private const string URL = "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2023-10-31-13-06/ffmpeg-n6.0-162-g07e3223dd0-win64-lgpl-shared-6.0.zip";
        
        public static void Download(string outputDirectory, bool overwrite = false)
        {
            if (Directory.Exists(outputDirectory))
            {
                if(overwrite)
                    Directory.Delete(outputDirectory, true);
                else
                    return;
            }
            if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);
            string outputArchive =
                Path.Combine(outputDirectory, "ffmpeg-n6.0-162-g07e3223dd0-win64-lgpl-shared-6.0.zip");
            using WebClient webClient = new WebClient();
            webClient.DownloadFile(URL, outputArchive);
            ZipFile.ExtractToDirectory(outputArchive, outputDirectory);
            string container = Directory.GetDirectories(outputDirectory)[0];
            string bin = Path.Combine(container, "bin");
            foreach (string lib in Directory.GetFiles(bin))
            {
                string fileName = Path.GetFileName(lib);
                string newFilePath = Path.Combine(outputDirectory, fileName);
                File.Move(lib, newFilePath);
            }
            Directory.Delete(container, true);
            File.Delete(outputArchive);
        }
    }
}