using System.Collections.Generic;
using TMPro;

namespace Hypernex.Tools.Text
{
    public interface IEmojiSheet
    {
        public List<string> GetUnicodeCharacters(TMP_SpriteAsset spriteAsset);
        public bool IsMatch(string unicode, string spriteAssetName);
    }
}