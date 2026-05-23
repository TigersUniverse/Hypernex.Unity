using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using i18next_net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Abstraction;

namespace Hypernex.Tools
{
    /// <summary>
    /// Handles anything translation-wise. Currently uses LibreTranslate to operate.
    /// </summary>
    public class TranslateCore : IDisposable
    {
        // Thank you, Mastodon
        private const string ENDPOINT = "https://translate.mstdn.social/translate";
        private const string RAW_REPO =
            "https://raw.githubusercontent.com/HypernexTeam/UnityTranslation/refs/heads/main";

        public static string ClientLanguage => CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
        public static List<ITranslateElement> TranslateElements = new List<ITranslateElement>();
        private static i18next i18Next;
        
        private HttpClient client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        internal static async void Init(string translationFile, string pathToPersistentData)
        {
            using HttpClient client = new HttpClient();
            string url = $"{RAW_REPO}/locales/{ClientLanguage}/translation.json";
            HttpResponseMessage response = await client.GetAsync(url);
            if(response.IsSuccessStatusCode)
                await File.WriteAllTextAsync(translationFile, await response.Content.ReadAsStringAsync());
            if(!File.Exists(translationFile)) return;
            i18Next = new i18next(new InitOptions
            {
                defaultNS = "translation",
                localeFileType = LocaleFileTypeEnum.Path,
                fallbackLng = "en",
                overridePath = pathToPersistentData
            });
            i18Next.changeLanguage(ClientLanguage);
            TranslateElements.ForEach(x => x.Translate());
        }

        #nullable enable
        public static string? GetStaticTranslation(string path)
        {
            if (i18Next == null) return null;
            string val = i18Next.t("translation:" + path, new {});
            UnityEngine.Debug.Log("value " + val);
            if (string.IsNullOrEmpty(val)) return null;
            return val;
        }
        #nullable restore

        public async Task<string> GetTranslation(string sourceText, string from, string to)
        {
            var payload = new
            {
                q = sourceText,
                source = from,
                target = to,
                format = "text"
            };
            string json = JsonConvert.SerializeObject(payload);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(ENDPOINT, content);
            string resultJson = await response.Content.ReadAsStringAsync();
            JObject obj = JObject.Parse(resultJson);
            string r = obj["translatedText"]?.ToString();
            return r;
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}