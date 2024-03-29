﻿using System.Runtime.InteropServices;

namespace Azure.Functions.Testing;

internal class ProjectFunctionLocator : IFunctionLocator
{
    private static readonly string[] SearchPaths = new[]
    {
        Environment.CurrentDirectory,
        AppDomain.CurrentDomain.RelativeSearchPath,
        AppDomain.CurrentDomain.BaseDirectory
    };

    private readonly string _funcProjectFolder;

    public ProjectFunctionLocator(string funcProjectFolder)
    {
        _funcProjectFolder = funcProjectFolder;
    }

    public string StartupDirectory => FindStartupDirectory();

    private string FindStartupDirectory()
    {
        var solutionFolder = GetSolutionFolderPath();
        return FindSubFolderPath(solutionFolder, _funcProjectFolder);
    }

    private static string GetSolutionFolderPath()
    {
        foreach (var solutionPath in SearchPaths.Select(FindSolution).Where(solutionPath => solutionPath != null))
        {
            return solutionPath!.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate applications solution file.");
    }

    private static DirectoryInfo? FindSolution(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        var directory = new DirectoryInfo(path);

        while (directory != null && directory.GetFiles("*.sln").Length == 0)
        {
            directory = directory.Parent;
        }

        return directory;
    }

    private static string FindSubFolderPath(string rootFolderPath, string folderName)
    {
        if (string.IsNullOrEmpty(rootFolderPath))
        {
            throw new DirectoryNotFoundException(rootFolderPath);
        }

        var rootDirectory = new DirectoryInfo(rootFolderPath);

        var directory = (rootDirectory.GetDirectories("*", SearchOption.AllDirectories))
            .FirstOrDefault(folder => !IsInIgnoredFolder(rootDirectory, folder) && string.Equals(folder.Name, folderName, StringComparison.OrdinalIgnoreCase));

        if (directory == null)
        {
            throw new DirectoryNotFoundException($"Unable to find project folder {folderName} under solution directory {rootFolderPath}");
        }

        return directory.FullName;
    }

    private static bool IsInIgnoredFolder(DirectoryInfo rootDirectory, DirectoryInfo directory)
    {
        var parent = directory.Parent;
        while (parent != null && parent.FullName != rootDirectory.FullName)
        {
            if (parent.Name.StartsWith("."))
            {
                return true;
            }
            parent = parent.Parent;
        }

        return false;
    }
}