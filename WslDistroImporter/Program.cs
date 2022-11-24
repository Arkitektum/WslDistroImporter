using CommandLine;
using CommandLine.Text;
using WslDistroImporter;
using static WslDistroImporter.Helpers;

var parser = new Parser(settings =>
{
    settings.HelpWriter = null;
    settings.IgnoreUnknownArguments = false;
});

var commandLineArgs = GetCommandLineArgs();
        
var parserResult = parser.ParseArguments<Options>(commandLineArgs);

var exitCode = parserResult
    .MapResult(
        ProcessRunner.ImportDistro, 
        errors => DisplayHelp(parserResult, errors)
    );

return exitCode;

static int DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errors)
{
    var helpText = HelpText.AutoBuild(result, helpText =>
    {
        helpText.AdditionalNewLineAfterOption = false;
        helpText.Heading = "WSL 2 Linux Distro Importer";
        helpText.Copyright = string.Empty;

        return HelpText.DefaultParsingErrorsHandler(result, helpText);
    }, example => example);

    Console.WriteLine(helpText);
    return 0;
}
