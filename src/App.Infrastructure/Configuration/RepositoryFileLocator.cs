namespace App.Infrastructure;

internal static class RepositoryFileLocator
{
    public static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var gitPath = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "The repository root could not be found. Configure Artifacts:Root with an absolute path.");
    }

    public static string FindFile(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var path = segments.Aggregate(directory.FullName, Path.Combine);
            if (File.Exists(path)) return path;
            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Required application file was not found: {string.Join(Path.DirectorySeparatorChar, segments)}");
    }

    public static string FindDirectory(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var path = segments.Aggregate(directory.FullName, Path.Combine);
            if (Directory.Exists(path)) return path;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Required application directory was not found: {string.Join(Path.DirectorySeparatorChar, segments)}");
    }
}
