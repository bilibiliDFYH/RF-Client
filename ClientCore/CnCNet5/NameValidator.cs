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
                return "Please enter a name.".L10N("UI:ClientCore:EnterAName");

            if (profanityFilter.IsOffensive(name))
                return "Please enter a name that is less offensive.".L10N("UI:ClientCore:NameOffensive");

            if (name.Contains(' '))
                return "用户名不能包含空格。";

            if (name.EndsWith('_'))
                return "用户名不能以下划线结尾。";

            if (name.Length > ClientConfiguration.Instance.MaxNameLength)
                return "Your nickname is too long.".L10N("UI:ClientCore:NameTooLong");

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
