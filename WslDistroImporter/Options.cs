using CommandLine;

namespace WslDistroImporter
{
    public class Options
    {
        [Option('n', "distro-name", Required = true, HelpText = "Distro name")]
        public string DistroName { get; set; }
        [Option('i', "image", SetName = "image", Required = true, HelpText = "Docker image")]
        public string Image { get; set; }
        [Option('f', "dockerfile", SetName = "dockerfile", Required = true, HelpText = "Path of Dockerfile")]
        public string Dockerfile { get; set; }
        [Option('l', "install-location", Required = true, HelpText = "Install location")]
        public string InstallLocation { get; set; }
    }
}
