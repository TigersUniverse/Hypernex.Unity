using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace Hypernex.Tools.Text
{
    public class NotoEmoji : IEmojiSheet
    {
        public List<string> GetUnicodeCharacters(TMP_SpriteAsset spriteAsset)
        {
            List<string> unicodeCharacters = new List<string>();
            foreach (TMP_SpriteGlyph tmpSpriteGlyph in spriteAsset.spriteGlyphTable)
            {
                tmpSpriteGlyph.metrics = new GlyphMetrics(tmpSpriteGlyph.metrics.width, tmpSpriteGlyph.metrics.height,
                    0, 60, tmpSpriteGlyph.metrics.horizontalAdvance);
            }
            foreach (TMP_SpriteCharacter spriteCharacter in spriteAsset.spriteCharacterTable)
            {
                string s = @"\U000" + spriteCharacter.name.Substring(1).Replace("_", @"\U000").ToUpper();
                unicodeCharacters.Add(s);
            }
            return unicodeCharacters;
        }

        public bool IsMatch(string unicode, string spriteAssetName)
        {
            string sub = "u" + unicode.Substring(5).Replace(@"\U000", "_");
            return sub.ToLower() == spriteAssetName.ToLower();
        }
    }
}