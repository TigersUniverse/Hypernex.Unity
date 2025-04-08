using System.IO;
using System.IO.Compression;
using System.Net;

namespace Hypernex.Tools
{
    public static class FFMpegDownloader
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        // win64
        // This build is LGPL, see https://ffmpeg.org/legal.html
        private const string URL = "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2023-07-31-12-50/ffmpeg-n6.0-32-gd4a7a6e7fa-win64-lgpl-shared-6.0.zip";
#else
        // linux64
        // This build is LGPL, see https://ffmpeg.org/legal.html
        private const string URL = "https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2023-07-31-12-50/ffmpeg-n6.0-32-gd4a7a6e7fa-linux64-lgpl-shared-6.0.tar.xz";
#endif
        
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
                Path.Combine(outputDirectory, "ffmpeg-n6.0-32-gd4a7a6e7fa-win64-lgpl-shared-6.0.zip");
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