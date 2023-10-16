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
}
