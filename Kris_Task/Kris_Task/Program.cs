using System;
using System.IO;
using System.Linq;
using System.Threading;

class FolderSynchronizer
{
    static void Main(string[] args)
    {
        // Check if the correct number of command line arguments are provided
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: FolderSynchronizer.exe <sourceFolderPath> <replicaFolderPath> <logFilePath>");
            return;
        }

        string sourceFolderPath = args[0];
        string replicaFolderPath = args[1];
        string logFilePath = args[2];
        int synchronizationIntervalSeconds = 60; // Default synchronization interval

        Console.WriteLine($"Source Folder: {sourceFolderPath}");
        Console.WriteLine($"Replica Folder: {replicaFolderPath}");
        Console.WriteLine($"Log File: {logFilePath}");

        // Create log file if it does not exist
        if (!File.Exists(logFilePath))
        {
            File.Create(logFilePath).Close();
        }

        Console.WriteLine("Synchronization started...");

        // Run synchronization loop
        while (true)
        {
            SynchronizeFolders(sourceFolderPath, replicaFolderPath, logFilePath);

            // Sleep for the specified interval before the next synchronization
            Thread.Sleep(synchronizationIntervalSeconds * 1000);
        }
    }

    static void SynchronizeFolders(string sourceFolderPath, string replicaFolderPath, string logFilePath)
    {
        try
        {
            // Log synchronization start time
            LogToFile(logFilePath, $"Synchronization started at {DateTime.Now}");

            // Delete files and folders in replica that do not exist in source
            DeleteExtraFilesAndFolders(sourceFolderPath, replicaFolderPath, logFilePath);

            // Copy new and modified files from source to replica
            CopyNewAndModifiedFiles(sourceFolderPath, replicaFolderPath, logFilePath);

            // Log synchronization completion time
            LogToFile(logFilePath, $"Synchronization completed at {DateTime.Now}");
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur during synchronization
            LogToFile(logFilePath, $"Error during synchronization: {ex.Message}");
        }
    }

    static void DeleteExtraFilesAndFolders(string sourceFolderPath, string replicaFolderPath, string logFilePath)
    {
        // Get files and folders in replica
        var replicaItems = Directory.GetFileSystemEntries(replicaFolderPath, "*", SearchOption.AllDirectories);

        // Delete files and folders in replica that do not exist in source
        foreach (var item in replicaItems)
        {
            string relativePath = item.Substring(replicaFolderPath.Length + 1);
            string sourceItemPath = Path.Combine(sourceFolderPath, relativePath);

            if (!File.Exists(sourceItemPath) && !Directory.Exists(sourceItemPath))
            {
                LogToFile(logFilePath, $"Deleting: {relativePath}");
                File.Delete(item);
            }
        }
    }

    static void CopyNewAndModifiedFiles(string sourceFolderPath, string replicaFolderPath, string logFilePath)
    {
        // Get files in source
        var sourceFiles = Directory.GetFiles(sourceFolderPath, "*", SearchOption.AllDirectories);

        // Copy new and modified files from source to replica
        foreach (var sourceFile in sourceFiles)
        {
            string relativePath = sourceFile.Substring(sourceFolderPath.Length + 1);
            string replicaFilePath = Path.Combine(replicaFolderPath, relativePath);

            // Check if the file exists in replica and is different from the source
            if (File.Exists(replicaFilePath) && File.GetLastWriteTimeUtc(replicaFilePath) != File.GetLastWriteTimeUtc(sourceFile))
            {
                LogToFile(logFilePath, $"Updating: {relativePath}");
                File.Copy(sourceFile, replicaFilePath, true);
            }
            // If the file does not exist in replica, copy it
            else if (!File.Exists(replicaFilePath))
            {
                LogToFile(logFilePath, $"Copying: {relativePath}");
                File.Copy(sourceFile, replicaFilePath);
            }
        }
    }

    static void LogToFile(string logFilePath, string logMessage)
    {
        // Log the message to both the console and the log file
        Console.WriteLine(logMessage);
        File.AppendAllText(logFilePath, $"{logMessage}{Environment.NewLine}");
    }
}
