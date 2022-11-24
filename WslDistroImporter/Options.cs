using CommandLine;

namespace WslDistroImporter
{
    public class Options
    {
        [Option('n', "distro-name", Required = true, HelpText = "Your WSL distro name.")]
        public string DistroName { get; set; }
        [Option('i', "image", SetName = "image", Required = true, HelpText = "Docker image. Must be present locally or at hub.docker.com.")]
        public string Image { get; set; }
        [Option('f', "dockerfile", SetName = "dockerfile", Required = true, HelpText = "Path of Dockerfile.")]
        public string Dockerfile { get; set; }
        [Option('l', "install-location", Required = true, HelpText = "Where to install WSL distro.")]
        public string InstallLocation { get; set; }
    }
}
