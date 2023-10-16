using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackData
{
    public int attackerID, attackeeID;
    // We record all shots and all hits. If it was a hit, it'll be indicated by this variable.
    // Not all shots are hits and the same attack will register as both a hit and a shot
    public int isHit; // 1 if hit, 0 otherwise (note: this does *NOT* Mean it was a miss)
    public int episodeCount, stepCount;
    public float timescale;
    long time;

    private string d = "\t"; // delimiter

    public AttackData(int attackerID, int attackeeID, int isHit, int episodeCount, int stepCount, float timescale)
    {
        this.attackerID = attackerID;
        this.attackeeID = attackeeID;
        this.isHit = isHit;
        this.episodeCount = episodeCount;
        this.stepCount = stepCount;
        this.timescale = timescale;
        time = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
    }

    public override string ToString()
    {
        return attackerID + d + attackeeID + d + isHit.ToString() + d + episodeCount + d + stepCount + d + timescale.ToString("#.##") + d + time.ToString() + "\n";
    }
}

public class StatisticsController : MonoBehaviour
{
    public bool collectStatistics = true;
    private List<AttackData> attackDataList = new List<AttackData>();

    [Header("Data File Analysis")]
    public TextAsset dataToAnalyze;
    public bool analyzeFile = false;

    void Awake()
    {
        if (collectStatistics)
        {
            ProjectileController.ProjectileShot += CollectShootData;
            ProjectileBullet.HitByProjectile += CollectHitData;
        }
    }

    private void OnDestroy()
    {
        if (collectStatistics)
        {
            ProjectileController.ProjectileShot -= CollectShootData;
            ProjectileBullet.HitByProjectile -= CollectHitData;
        }
    }

    protected virtual void Update()
    {
        if (analyzeFile)
        {
            RunAnalytics(dataToAnalyze.text);
            analyzeFile = false;
        }
    }

    private void CollectShootData(ScoutAgent shooter, ScoutAgent target, ProjectileBullet pb)
    {
        AttackData ad = new AttackData(
            shooter.transform.GetSiblingIndex(),
            target.transform.GetSiblingIndex(),
            0,
            shooter.CompletedEpisodes,
            shooter.StepCount,
            Time.timeScale);
        attackDataList.Add(ad);
    }

    private void CollectHitData(ScoutAgent shooter, ScoutAgent target, ProjectileBullet pb)
    {
        AttackData ad = new AttackData(
            shooter.transform.GetSiblingIndex(),
            target.transform.GetSiblingIndex(),
            1,
            shooter.CompletedEpisodes,
            shooter.StepCount,
            Time.timeScale);
        attackDataList.Add(ad);
    }

    private void OnApplicationQuit()
    {
        if (collectStatistics)
        {
            // Write Data
            string outputData = "index\tattackerID\tattackeeID\tisHit\tepisodeCount\tstepCount\ttimescale\ttime\n";
            for (int i = 0; i < attackDataList.Count; ++i)
            {
                outputData += i + "\t" + attackDataList[i].ToString();
            }
            long time = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
            System.IO.File.WriteAllText("C:\\test\\ScoutAttackData_" + time + ".txt", outputData);
        }
    }

    #region FILE_ANALYTICS

    /// <summary>
    /// 0 index
    /// 1 attackerID
    /// 2 attackeeID
    /// 3 isHit
    /// 4 episodeCount
    /// 5 stepCount
    /// 6 timescale
    /// 7 time
    /// </summary>
    /// <param name="dataset"></param>
    /// <param name="hasHeader"></param>
    private void RunAnalytics(string dataset, bool hasHeader = true)
    {
        string output = "";
        string[] lines = dataset.Split('\n');
        float currentTimeScale = 1f;
        int numShots = 0, numHits = 0;
        for (int i = hasHeader ? 1 : 0; i < lines.Length; ++i)
        {
            string line = lines[i].Trim();
            if (!string.IsNullOrEmpty(line))
            {
                string[] parts = line.Split('\t');
                int isHit = int.Parse(parts[3]);
                float timeScale = float.Parse(parts[6]);
                if (timeScale != currentTimeScale)
                {
                    output += currentTimeScale.ToString() + "\t" + numHits + "/" + numShots + "\t" + ((float)numHits / (float)numShots) + "\n";
                    numShots = 0;
                    numHits = 0;
                    currentTimeScale = timeScale;
                }
                if (isHit == 0)
                    numShots++;
                if (isHit == 1)
                    numHits++;
            }
        }
        output += currentTimeScale.ToString() + "\t" + numHits + "/" + numShots + "\t" + ((float)numHits / (float)numShots) + "\n";

        Debug.Log(output);
    }

    #endregion

}
