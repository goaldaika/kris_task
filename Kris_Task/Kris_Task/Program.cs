using System;
using System.IO;
using System.Security.Cryptography;
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

        // Create a separate thread for synchronization
        Thread syncThread = new Thread(() =>
        {
            while (true)
            {
                SynchronizeFolders(sourceFolderPath, replicaFolderPath, logFilePath);
                Thread.Sleep(synchronizationIntervalSeconds * 1000);
            }
        });

        // Start the synchronization thread
        syncThread.Start();

        // Allow the user to exit the program by pressing a key
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

        // Stop the synchronization thread
        syncThread.Abort();
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
            if (File.Exists(replicaFilePath) && !IsFileContentEqual(sourceFile, replicaFilePath))
            {
                LogToFile(logFilePath, $"Updating: {relativePath}");
                CopyFileWithAttributes(sourceFile, replicaFilePath);
            }
            // If the file does not exist in replica, copy it
            else if (!File.Exists(replicaFilePath))
            {
                LogToFile(logFilePath, $"Copying: {relativePath}");
                CopyFileWithAttributes(sourceFile, replicaFilePath);
            }
        }
    }

    static bool IsFileContentEqual(string filePath1, string filePath2)
    {
        string hash1 = CalculateMD5(filePath1);
        string hash2 = CalculateMD5(filePath2);

        return hash1.Equals(hash2, StringComparison.OrdinalIgnoreCase);
    }

    static void CopyFileWithAttributes(string sourceFilePath, string destinationFilePath)
    {
        using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
        using (var destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write))
        {
            // Copy file using a buffer
            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                destinationStream.Write(buffer, 0, bytesRead);
            }
        }

        // Copy file attributes (including permissions and ownership)
        File.SetAttributes(destinationFilePath, File.GetAttributes(sourceFilePath));

        // Note: This may not handle all edge cases related to file permissions and ownership, depending on the operating system and file system.
    }

    static string CalculateMD5(string filePath)
    {
        using (var md5 = MD5.Create())
        using (var stream = File.OpenRead(filePath))
        {
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }

    static void LogToFile(string logFilePath, string logMessage)
    {
        // Enhance log entries with timestamps
        string formattedLog = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {logMessage}";

        // Log the message to both the console and the log file
        Console.WriteLine(formattedLog);
        File.AppendAllText(logFilePath, $"{formattedLog}{Environment.NewLine}");
    }
}
