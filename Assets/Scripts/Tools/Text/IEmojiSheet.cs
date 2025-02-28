using System.Collections.Generic;
using TMPro;

namespace Hypernex.Tools.Text
{
    public interface IEmojiSheet
    {
        public List<string> UnicodeCharacters { get; }
        public void InitializeUnicodeCharacters(TMP_SpriteAsset spriteAsset);
        public bool IsMatch(string unicode, string spriteAssetName);
    }
}