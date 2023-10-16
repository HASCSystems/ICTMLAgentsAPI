using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StringUtility : MonoBehaviour
{
    /// <summary>
    /// Convert a string in (#.##, #.##, #.##) format to a Vector3.
    /// Note: there is no format checking in this function. Please check format before converting.
    /// </summary>
    /// <param name="vectorAsString">In (#.##, #.##, #.##) format</param>
    /// <returns>Vector3 representation of the string.</returns>
    public static Vector3 StringToVector3(string vectorAsString, char delimiter = ',')
    {
        string v = vectorAsString.Replace("(", "").Replace(")", "");
        string[] vcomps = v.Split(delimiter);
        Vector3 vec = new Vector3();
        for (int i=0; i<vcomps.Length; ++i)
        {
            float val = float.Parse(vcomps[i].Trim());
            vec[i] = val;
        }
        return vec;
    }

    public static int[] StringToIntPair(string intPairAsCartesian)
    {
        try
        {

            string v = intPairAsCartesian.Replace("(", "").Replace(")", "");
            string[] vcomps = v.Split(',');
            int[] pair = new int[2];
            for (int i = 0; i < vcomps.Length; ++i)
            {
                int val = int.Parse(vcomps[i].Trim());
                pair[i] = val;
            }
            return pair;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Incorrect Pair Format: " + intPairAsCartesian);
            return new int[2];
        }
    }

    public static string IntPairToString(int[] intPair)
    {
        return IntPairToString(intPair[0], intPair[1]);
    }

    public static string IntPairToString(int x, int y)
    {
        return "(" + x + "," + y + ")";
    }

    public static bool IsIDBetweenSWandNEcorners(string trialID, string swID, string neID)
    {
        int[] trialPair = StringToIntPair(trialID);
        int[] swPair = StringToIntPair(swID);
        int[] nePair = StringToIntPair(neID);

        return
            (trialPair[0] >= swPair[0] && trialPair[0] <= nePair[0] &&
             trialPair[1] >= swPair[1] && trialPair[1] <= nePair[1]);
    }
}
