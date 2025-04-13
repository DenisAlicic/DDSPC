namespace DDSPC.Util;

public static class ProjectPathHelper
{
    public static string GetProjectRootPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var dirInfo = new DirectoryInfo(currentDir);

        while (dirInfo != null && !dirInfo.GetFiles("*.sln").Any())
        {
            dirInfo = dirInfo.Parent;
        }

        return dirInfo?.FullName ?? currentDir;
    }

    public static string GetDataPath()
    {
        return Path.Combine(GetProjectRootPath(), "Data");
    }
}