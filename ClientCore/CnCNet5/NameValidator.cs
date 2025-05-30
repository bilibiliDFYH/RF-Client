using System;
using System.Linq;
using System.Text;
using Localization;

namespace ClientCore.CnCNet5
{
    public static class NameValidator
    {
        private static readonly Encoding GBKEncoding = Encoding.GetEncoding("GBK");
        private static readonly char[] AllowedAsciiCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_[]|\\{}^`".ToCharArray();

        /// <summary>
        /// Checks if the player's nickname is valid for CnCNet.
        /// </summary>
        public static string IsNameValid(string name)
        {
            var profanityFilter = new ProfanityFilter();

            if (string.IsNullOrEmpty(name))
                return "Please enter a name.".L10N("UI:ClientCore:EnterAName");

            if (profanityFilter.IsOffensive(name))
                return "Please enter a name that is less offensive.".L10N("UI:ClientCore:NameOffensive");

            if (int.TryParse(name.Substring(0, 1), out _))
                return "The first character in the player name cannot be a number.".L10N("UI:ClientCore:NameFirstIsNumber");

            if (name[0] == '-')
                return "The first character in the player name cannot be a dash ( - ).".L10N("UI:ClientCore:NameFirstIsDash");

            if (name.Contains(' '))
                return "The player name cannot contain spaces.".L10N("UI:ClientCore:NameHasSpace");

            if (name.EndsWith('_'))
                return "The player name cannot end with an underline ( _ ).".L10N("UI:ClientCore:NameEndOfUnderline");

            // 检查字符有效性
            foreach (char c in name)
            {
                // 检查是否是允许的ASCII字符
                if (AllowedAsciiCharacters.Contains(c))
                    continue;

                byte[] bytes = GBKEncoding.GetBytes(new[] { c });
                if (bytes.Length == 2)
                {
                    byte b1 = bytes[0];
                    byte b2 = bytes[1];

                    // 简体中文 GB2312 范围
                    bool isSimplified =
                        (b1 >= 0xB0 && b1 <= 0xD6 && b2 >= 0xA1 && b2 <= 0xFE) ||
                        (b1 == 0xD7 && b2 >= 0xA1 && b2 <= 0xF9) ||
                        (b1 >= 0xD8 && b1 <= 0xF7 && b2 >= 0xA1 && b2 <= 0xFE);

                    // 繁体中文 GBK 扩展区
                    bool isTraditional =
                        (b1 >= 0x81 && b1 <= 0xA0 && ((b2 >= 0x40 && b2 <= 0x7E) || (b2 >= 0x80 && b2 <= 0xFE))) ||
                        (b1 >= 0xAA && b1 <= 0xFE && ((b2 >= 0x40 && b2 <= 0x7E) || (b2 >= 0x80 && b2 <= 0xA0)));

                    // 主要GBK双字节范围
                    bool isGBKMain = (b1 >= 0x81 && b1 <= 0xFE && b2 >= 0x40 && b2 <= 0xFE);

                    // GBK扩展双字节范围
                    bool isGBKExt = (b1 >= 0x81 && b1 <= 0xFE && b2 >= 0x80 && b2 <= 0xFE);

                    if (isSimplified || isTraditional || isGBKMain || isGBKExt)
                        continue;
                }

                return "Your player name has invalid characters in it.".L10N("UI:ClientCore:NameInvalidChar1") + Environment.NewLine +
                       "Allowed characters are A-Z, numbers, and Chinese characters (Simplified/Traditional) encoded in GBK.".L10N("UI:ClientCore:NameInvalidChar2");
            }

            if (name.Length > ClientConfiguration.Instance.MaxNameLength)
                return $"您的名称过长,不得超过 {ClientConfiguration.Instance.MaxNameLength} 个字符";

            return null;
        }

        /// <summary>
        /// Returns player nickname constrained to maximum allowed length and with invalid characters for offline nicknames removed.
        /// Does not check for offensive words or invalid characters for CnCNet.
        /// </summary>
        /// <param name="name">Player nickname.</param>
        /// <returns>Player nickname with invalid offline nickname characters removed and constrained to maximum name length.</returns>
        public static string GetValidOfflineName(string name)
        {
            char[] disallowedCharacters = ",;".ToCharArray();

            string validName = new string(name.Trim().Where(c => !disallowedCharacters.Contains(c)).ToArray());

            if (validName.Length > ClientConfiguration.Instance.MaxNameLength)
                return validName.Substring(0, ClientConfiguration.Instance.MaxNameLength);
            
            return validName;
        }

        /// <summary>
        /// Checks if a game name is valid for CnCNet.
        /// </summary>
        /// <param name="gameName">Game name.</param>
        /// <returns>Null if the game name is valid, otherwise a string that tells
        /// what is wrong with the name.</returns>
        public static string IsGameNameValid(string gameName)
        {
            if (string.IsNullOrEmpty(gameName))
            {
                return "Please enter a game name.".L10N("UI:ClientCore:GameNameMissing");
            }

            char[] disallowedCharacters = { ',', ';' };
            if (gameName.IndexOfAny(disallowedCharacters) != -1)
            {
                return "Game name contains disallowed characters.".L10N("UI:ClientCore:GameNameDisallowedChars");
            }

            if (gameName.Length > 23)
            {
                return "Game name is too long.".L10N("UI:ClientCore:GameNameTooLong");
            }

            if (new ProfanityFilter().IsOffensive(gameName))
            {
                return "Please enter a less offensive game name.".L10N("UI:ClientCore:GameNameOffensiveText");
            }

            return null;
        }
    }
}