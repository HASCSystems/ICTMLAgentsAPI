using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FileLoader : MonoBehaviour
{
    public TextAsset dataFile;
    public GameObject graphWaypointPrefab;

    private void Start()
    {
        LoadFromGraphNeuralFile();
    }

    private void LoadFromGraphNeuralFile()
    {
        string[] lines = dataFile.text.Split('\n');
        for (int i=0; i<lines.Length; ++i)
        {
            string line = lines[i];
            if (!string.IsNullOrEmpty(line.Trim()))
            {
                string[] parts = line.Split('\t');

                Vector3 loc = StringUtility.StringToVector3(parts[1].Trim());
                GameObject wp = GameObject.Instantiate(graphWaypointPrefab, transform);
                wp.transform.position = loc;
                wp.name = "N" + (i + 1) + parts[0];
            }
        }
    }

    public static void WriteLineToFile(string fileName, string lineToWrite)
    {
        string path = Application.streamingAssetsPath + "\\" + fileName;
        // This text is added only once to the file.
        if (!File.Exists(path))
        {
            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine(lineToWrite);
            }
        }
        else
        {
            // This text is always added, making the file longer over time
            // if it is not deleted.
            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine(lineToWrite);
            }
        }
    }

    public static string RetrieveFileContents(string fileName)
    {
        string path = Application.streamingAssetsPath + "\\" + fileName;
        // This text is added only once to the file.
        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }
        else
        {
            return string.Empty;
        }
    }
}
