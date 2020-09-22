using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;

namespace Utili
{
    internal class TimespanTypeReader : TypeReader
    {
        private static Regex TimeSpanRegex { get; } = new Regex(@"^(?<days>\d+d)?(?<hours>\d{1,2}h)?(?<minutes>\d{1,2}m)?(?<seconds>\d{1,2}s)?$", RegexOptions.Compiled);
        private static string[] RegexGroups { get; } = { "days", "hours", "minutes", "seconds" };

        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            await Task.Yield();

            TimeSpan result = TimeSpan.Zero;
            if (input == "0")
                return TypeReaderResult.FromSuccess((TimeSpan?)null);

            if (TimeSpan.TryParse(input, out result))
                return TypeReaderResult.FromSuccess(result);

            Match mtc = TimeSpanRegex.Match(input);
            if (!mtc.Success)
                return TypeReaderResult.FromError(CommandError.ParseFailed, "Invalid TimeSpan string");

            int d = 0;
            int h = 0;
            int m = 0;
            int s = 0;
            foreach (string gp in RegexGroups)
            {
                string gpc = mtc.Groups[gp].Value;
                if (string.IsNullOrWhiteSpace(gpc))
                    continue;

                char gpt = gpc.Last();
                int.TryParse(gpc.Substring(0, gpc.Length - 1), out int val);
                switch (gpt)
                {
                    case 'd':
                        d = val;
                        break;

                    case 'h':
                        h = val;
                        break;

                    case 'm':
                        m = val;
                        break;

                    case 's':
                        s = val;
                        break;
                }
            }
            result = new TimeSpan(d, h, m, s);
            return TypeReaderResult.FromSuccess(result);
        }
    }
}