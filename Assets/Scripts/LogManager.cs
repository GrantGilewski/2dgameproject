using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LogManager : MonoBehaviour
{
    // Singleton instance thats able to be globally accessed
    public static LogManager instance = null;

    public static string logName = string.Empty; // Log Name

    public static StreamWriter logFile; // Direct reference to log file

    // ERROR [0] > WARNING [1] > INFO [2] > DEBUG [3]
    public static int ERROR = 0;
    public static int WARNING = 1;
    public static int INFO = 2;
    public static int DEBUG = 3;

    private static String[] logLevels = {"ERROR", "WARNING", "INFO", "DEBUG"};

    // logLevel writes all logs that are the set level or LOWER
    public static int fileLevel = INFO;

    // consoleLevel displays all logs that are the set level or LOWER
    public static int consoleLevel = DEBUG;

    // Initialize code
    private void Awake()
    {
        // If instance does not exist, set it to this object
        if (instance == null)
            instance = this;

        // If instance does exist and it is not this, destroy this. Only 1 instance can exist
        else if (instance != this)
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        // Create Log File
        createLog();
    }

    private void createLog()
    {
        // If within valid range, create file log
        if (fileLevel >= 0 && fileLevel < 3)
        {
            // Current Timestamp
            DateTime date = DateTime.UtcNow;
            logName = date.ToString(new CultureInfo("en-US"));

            // Replace slash with dash to prevent OS name conflicts
            logName = logName.Replace("/", "-");
            // Colon with underscore
            logName = logName.Replace(":", "_");

            // Add file extension
            logName += ".txt";

            // Add Logs subfolder
            logName = "logs/" + logName;

            logFile = File.CreateText(logName);

            // Directly write Log Level into log
            logFile.WriteLineAsync(("Log Level: " + logLevels[fileLevel]));

            // Directly write OS Info into log
            logFile.WriteLineAsync("Platform: " + Environment.OSVersion.Platform);
            logFile.WriteLineAsync("OS Version: " + Environment.OSVersion.VersionString);
            logFile.WriteLineAsync("OS Description: " + System.Runtime.InteropServices.RuntimeInformation.OSDescription);

            // Log that the app started
            log("Application Started!", INFO);
        }
    }

    // dest refers to destination, -1 = both, 0 = only console, 1 = only file
    public void log(string message, int level, int dest = -1)
    {
        // Write to console (if applicable)
        if (dest != 1)
        {
            // Only write if the written log level is less than or equal to the set level
            if (level <= consoleLevel){
                if (level == ERROR) {
                    Debug.LogError(message);
                } else if (level == WARNING)
                {
                    Debug.LogWarning(message);
                } else if (level == INFO)
                {
                    Debug.Log("[INFO]  " + message);
                } else if (level == DEBUG)
                {
                    Debug.Log("[DEBUG] " + message);
                }
            }
        }
        // Write to file (if applicable)
        if (dest != 0)
        {
            // Only write if the written log level is less than or equal to the set level
            if (level <= fileLevel){

                DateTime date = DateTime.UtcNow;

                // Time fractions of a second
                String timestamp = date.TimeOfDay.ToString().Substring(0, 8);

                // Write Log
                logFile.WriteLineAsync(timestamp + " [" + logLevels[level] + "] \t" + message);
            }
        }
    }

    private void OnApplicationQuit()
    {
        // Close Log
        log("Application Quit!", INFO);
        logFile.Close();
    }
}
