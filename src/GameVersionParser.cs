using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Reactor.VersionCheck.util
{
    // copied from https://github.com/NuclearPowered/Reactor.OxygenFilter/blob/c9387706d04bd2563d40ee5cefb2838e9defa222/Reactor.Greenhouse/Setup/GameVersionParser.cs, with permission
    public static class GameVersionParser
    {
        private static int IndexOf(this IReadOnlyCollection<byte> source, IReadOnlyCollection<byte> pattern)
        {
            for (var i = 0; i < source.Count; i++)
            {
                if (source.Skip(i).Take(pattern.Count).SequenceEqual(pattern))
                {
                    return i;
                }
            }

            return -1;
        }

        public static string Parse(string file)
        {
            var bytes = File.ReadAllBytes(file);

            var pattern = Encoding.UTF8.GetBytes("public.app-category.games");
            var index = bytes.IndexOf(pattern) + pattern.Length + 127;

            return Encoding.UTF8.GetString(bytes.Skip(index).TakeWhile(x => x != 0).ToArray());
        }
    }
}