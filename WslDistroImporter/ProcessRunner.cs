using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace WslDistroImporter
{
    public class ProcessRunner
    {
        private static readonly string _currentPath = AppContext.BaseDirectory;
        private static readonly Regex _wsl2InstalledRegex = new(@"^Default Version\: 2$", RegexOptions.Compiled);
        private static readonly Regex _distroNameRegex = new(@"^[\.\-_a-zøæåA-ZØÆÅ0-9]+$", RegexOptions.Compiled);

        public static int ImportDistro(Options options)
        {
            string image = null;
            string containerId = null;
            string installLocation = null;
            bool existingImage = false;

            Console.Write("WSL 2 Linux Distro Importer\n\n");

            try
            {
                FixPaths(options);

                if (!IsValidDistroName(options.DistroName))
                    return 1;

                if (!IsValidInstallLocation(options.InstallLocation))
                    return 1;

                if (CheckDockerInstalledAndRunning() != 0)
                    return 1;

                if (CheckWsl2InstalledAsync() != 0)
                    return 1;

                image = options.Image ?? Guid.NewGuid().ToString();

                if (options.Image != null)
                {
                    existingImage = IsExistingDockerImage(options.Image);
                }
                else if (BuildDockerImage(options.Dockerfile, image) != 0)
                {
                    return 1;
                }

                if (RunDockerImage(image, existingImage, options.Dockerfile != null) != 0)
                {
                    CleanUp(containerId, image, installLocation, existingImage);
                    return 1;
                }

                containerId = GetDockerContainerId(image);

                if (containerId == null)
                {
                    CleanUp(containerId, image, installLocation, existingImage);
                    return 1;
                }

                installLocation = Path.GetFullPath(Path.Combine(_currentPath, options.InstallLocation));

                if (ExportDockerContainer(containerId, installLocation) != 0)
                {
                    CleanUp(containerId, image, installLocation, existingImage);
                    return 1;
                }

                if (ImportDistro(options.DistroName, installLocation, containerId) != 0)
                {
                    CleanUp(containerId, image, installLocation, existingImage);
                    return 1;
                }

                CreateStartFile(options.DistroName, installLocation);
                CleanUp(containerId, image, installLocation, existingImage);

                Console.Write($"\nLinux distro '{options.DistroName}' successfully imported into WSL!\n\n");
                return 0;
            }
            catch
            {
                CleanUp(containerId, image, installLocation, existingImage);

                Console.Write($"Error: Could not import Linux distro '{options.DistroName}'\n\n");
                return 1;
            }
        }

        private static int CheckDockerInstalledAndRunning()
        {
            try
            {
                using var process = RunProcess("docker.exe", "ps");

                if (process.ExitCode == 0)
                    return 0;

                Console.Write("Error: Docker daemon is not running!\n");
                return 1;
            }
            catch
            {
                Console.Write("Error: docker.exe was not found! Is Docker Desktop installed?\n");
                return 1;
            }
        }

        private static int CheckWsl2InstalledAsync()
        {
            try
            {
                using var process = RunProcess("wsl.exe", "--status", Encoding.Unicode);

                if (process.ExitCode != 0)
                {
                    Console.Write("Error: wsl.exe was not found! Is Windows Subsystem for Linux installed?\n");
                    return 1;
                }

                string outputLine;

                while ((outputLine = process.StandardOutput.ReadLine()) != null)
                {
                    if (_wsl2InstalledRegex.IsMatch(outputLine))
                        return 0;
                }

                Console.Write("Error: WSL 2 is not enabled on the system!\n\n");
                return 1;
            }
            catch
            {
                Console.Write("Error: wsl.exe was not found! Is Windows Subsystem for Linux installed?\n\n");
                return 1;
            }
        }

        private static int BuildDockerImage(string dockerfile, string image)
        {
            var filePath = Path.GetFullPath(Path.Combine(_currentPath, dockerfile));
            Console.Write($"Building image from Dockerfile '{filePath}'. This may take a little while... ");

            if (!File.Exists(filePath))
            {
                Console.Write("Failed!\nError: Dockerfile was not found!\n");
                return 1;
            }

            using var process = RunProcess("docker.exe", $"build --tag {image} --file \"{filePath}\" .");

            if (process.ExitCode != 0)
            {
                Console.Write($"Failed!\nError: Could not build image from Dockerfile '{filePath}'!\n");
                return 1;
            }

            Console.Write("Done!\n");
            return 0;
        }

        private static int RunDockerImage(string image, bool existingImage, bool fromDockerfile)
        {
            if (existingImage)
                Console.Write($"Running Docker image '{image}'... ");
            else if (fromDockerfile)
                Console.Write($"Running previously built Docker image... ");
            else
                Console.Write($"Pulling and running Docker image '{image}'... ");

            using var process = RunProcess("docker.exe", $"run -t {image} uname");

            if (process.ExitCode == 125)
            {
                Console.Write($"Failed!\nError: Docker image '{image}' is not a Linux distro!\n");
                return 1;
            }
            else if (process.ExitCode != 0)
            {
                Console.Write($"Failed!\nError: Could not run Docker image '{image}'!\n");
                return 1;
            }

            Console.Write("Done!\n");
            return 0;
        }

        private static int ExportDockerContainer(string containerId, string outputLocation)
        {
            Console.Write($"Exporting Docker container '{containerId}' to tar archive... ");

            if (!Directory.Exists(outputLocation))
                Directory.CreateDirectory(outputLocation);

            var outputPath = Path.Combine(outputLocation, containerId + ".tar");
            using var process = RunProcess("docker.exe", $"export --output=\"{outputPath}\" {containerId}");

            if (process.ExitCode != 0)
            {
                Console.Write($"Failed!\nError: Could not export Docker container '{containerId}'!\n");
                return 1;
            }

            Console.Write("Done!\n");
            return 0;
        }

        private static int ImportDistro(string distroName, string installLocation, string containerId)
        {
            Console.Write($"Importing tar archive into WSL... ");
            using var process = RunProcess("wsl.exe", $"--import {distroName} \"{installLocation}\" \"{Path.Combine(installLocation, containerId + ".tar")}\"");

            if (process.ExitCode != 0)
            {
                Console.Write($"Failed!\nError: Could not import distro '{distroName}'!\n");
                return 1;
            }

            Console.Write("Done!\n");
            return 0;
        }

        private static bool IsExistingDockerImage(string image)
        {
            var split = image.Split(':');
            var tag = split.ElementAtOrDefault(1);
            var dockerImageExistsRegex = new Regex(@$"^{split[0]}\b.*?{tag ?? "<none>"}");

            using var process = RunProcess("docker.exe", $"image ls -a");
            string outputLine;

            while ((outputLine = process.StandardOutput.ReadLine()) != null)
            {
                var match = dockerImageExistsRegex.Match(outputLine);

                if (match.Success)
                    return true;
            }

            return false;
        }

        private static string GetDockerContainerId(string image)
        {
            using var process = RunProcess("docker.exe", "container ls -a");

            var containerIdRegex = new Regex($@"\b(?<container_id>\w+)\b.*?\b({image})\b");
            string outputLine;

            while ((outputLine = process.StandardOutput.ReadLine()) != null)
            {
                var match = containerIdRegex.Match(outputLine);

                if (match.Success)
                    return match.Groups["container_id"].Value;
            }

            Console.Write($"Error: Could not find container ID for Docker image '{image}'!\n");
            return null;
        }

        private static void CleanUp(string containerId, string image, string installLocation, bool existingImage)
        {
            DeleteDockerContainer(containerId);
            DeleteDockerImage(image, existingImage);
            DeleteTarArchive(installLocation, containerId);
        }

        private static void DeleteDockerContainer(string containerId)
        {
            if (containerId == null)
                return;

            using var process = RunProcess("docker.exe", $"container rm {containerId}");
        }

        private static void DeleteDockerImage(string image, bool existingImage)
        {
            if (image == null || existingImage)
                return;

            using var process = RunProcess("docker.exe", $"image rm {image}");
        }

        private static void DeleteTarArchive(string installLocation, string containerId)
        {
            if (installLocation == null || containerId == null)
                return;

            var filePath = Path.Combine(installLocation, containerId + ".tar");

            if (!File.Exists(filePath))
                return;

            try
            {
                File.Delete(filePath);
            }
            catch
            {
            }
        }

        private static bool IsValidDistroName(string distroName)
        {
            if (_distroNameRegex.IsMatch(distroName))
                return true;

            Console.Write($"Error: Invalid distro name! A distro name may only contain characters, numbers, hyphens, underscores and dots.\n");
            return false;
        }

        private static bool IsValidInstallLocation(string installLocation)
        {
            var filePath = Path.Combine(installLocation, "ext4.vhdx");

            if (!File.Exists(filePath))
                return true;

            Console.Write($"Error: The install location already has a Linux distro!\n");
            return false;
        }

        private static void CreateStartFile(string distroName, string installLocation)
        {
            var filePath = Path.Combine(installLocation, $"{distroName}.bat");
            var contents = $"@echo off\ntitle {distroName}\nwsl.exe -d {distroName}";

            File.WriteAllText(filePath, contents);
        }

        private static void FixPaths(Options options)
        {
            if (options.Dockerfile != null)
                options.Dockerfile = options.Dockerfile.TrimEnd('"', '\\');

            options.InstallLocation = options.InstallLocation.TrimEnd('"', '\\');
        }

        private static Process RunProcess(string fileName, string arguments, Encoding outputEncoding = null)
        {
            var process = new Process();

            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.StandardOutputEncoding = outputEncoding;

            process.Start();
            process.WaitForExit();

            return process;
        }
    }
}
