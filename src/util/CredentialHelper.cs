using System;
using System.Security.Authentication;
using System.Text;

namespace Reactor.VersionCheck.util
{
    public static class CredentialHelper
    {
        public static string[] GetSteamLogin()
        {
            string steamInfoB64 = DotEnv.Get("STEAM_LOGIN", "");
            if (steamInfoB64.Length == 0)
            {
                throw new AuthenticationException("No Steam credentials provided");
            }
            byte[] data = Convert.FromBase64String(steamInfoB64);
            string steamInfoDecoded = Encoding.UTF8.GetString(data);
            return steamInfoDecoded.Split(":", 2);
        }
    }
}