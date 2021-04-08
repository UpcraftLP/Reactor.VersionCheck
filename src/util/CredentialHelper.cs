using System;
using System.Security.Authentication;
using System.Text;

namespace Reactor.VersionCheck.util
{
    public static class CredentialHelper
    {
        public static string[] GetSteamCredentials()
        {
            string steamInfoB64 = DotEnv.Get("STEAM_LOGIN", "");
            return GetCredentials(steamInfoB64, () => "No Steam credentials provided");
        }

        public static string[] GetCredentials(string base64, Func<string> errorMessageFactory, string separator = ":")
        {
            if (base64.Length == 0) throw new AuthenticationException(errorMessageFactory.Invoke());
            byte[] data = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(data).Split(separator, 2);
        }
    }
}