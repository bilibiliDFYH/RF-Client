using System;
using System.Linq;
using Localization;

namespace ClientCore.CnCNet5
{
    public static class NameValidator
    {
        /// <summary>
        /// Checks if the player's nickname is valid for CnCNet.
        /// </summary>
        /// <returns>Null if the nickname is valid, otherwise a string that tells
        /// what is wrong with the name.</returns>
        public static string IsNameValid(string name)
        {
            var profanityFilter = new ProfanityFilter();

            if (string.IsNullOrEmpty(name))
                return "请输入昵称.";

            if (name.Contains(' '))
                return "用户名不能包含空格。";

            if (name.EndsWith('_'))
                return "用户名不能以下划线结尾。";

            if (char.IsDigit(name[0]))
                return "用户名不能以数字开头。";

            if (name.Length > ClientConfiguration.Instance.MaxNameLength)
                return $"你的用户名太长了,不能超过{ClientConfiguration.Instance.MaxNameLength}个字符.";

            if (profanityFilter.IsOffensive(name))
                return "名称违规,请换一个.";

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
    }
}
