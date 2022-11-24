using System.Text.RegularExpressions;

namespace WslDistroImporter
{
    public class Helpers
    {
        private static readonly Regex _argsRegex = 
            new(@"(?<match>[\w\:\.\\\-_]+)|""(?<match>[\w\s\:\.\\\-_]*)""|'(?<match>[\w\s\:\.\\\-_]*)'", RegexOptions.Compiled);

        public static string[] GetCommandLineArgs()
        {
            var commandLineArgs = Environment.GetCommandLineArgs();
            var commandLine = Environment.CommandLine;
            var argsString = commandLine.Remove(0, commandLineArgs[0].Length);
            
            return _argsRegex.Matches(argsString)
                .Cast<Match>()
                .Select(match => match.Groups["match"].Value)
                .ToArray();
        }
    }
}
