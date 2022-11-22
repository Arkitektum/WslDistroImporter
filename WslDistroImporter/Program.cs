using CommandLine;
using CommandLine.Text;
using WslDistroImporter;

var parser = new Parser(settings =>
{
    settings.HelpWriter = null;
    settings.IgnoreUnknownArguments = false;
});

var parserResult = parser.ParseArguments<Options>(args);

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
