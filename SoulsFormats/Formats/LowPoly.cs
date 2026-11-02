using System;
using System.Collections.Generic;
using System.Numerics;

namespace SoulsFormats.Formats
{
    /// <summary>
    /// Found in Elden Ring Tarnished Edition (Switch 2)
    /// Basic format seemingly only for testing terrain models
    /// </summary>
    public class LowPoly
    {
        List<Vector3> posList = new();
        List<Vector3> nrmList = new();
        List<Vector2> uvList = new();
        List<Vector3> vertNrmList = new();
        List<int[]> faceList = new();
        public LowPoly(string[] lines)
        {
            Vector3 tempPos = new();
            Vector3 tempNrm = new();
            Vector2 tempUv = new();
            Vector3 tempVNrm = new();
            int[] tempFace = new int[0];
            foreach (string line in lines)
            {
                if (!line.Contains("SLOD.ProxyModel."))
                {
                    continue;
                }
                var lineSplit = line.Replace("SLOD.ProxyModel.", "").Split('=');
                var value = lineSplit[1];
                var lineSet = lineSplit[0].Split('[');
                var lineName = lineSet[0];
                string lineIndex;
                string lineAxis;
                float fltVal;
                switch (lineName.ToLower())
                {
                    case "vertexnum":
                        var count = Int32.Parse(value);
                        posList = new List<Vector3>(count);
                        vertNrmList = new List<Vector3>(count);
                        uvList = new List<Vector2>(count);
                        break;
                    case "facenum":
                        var fccount = Int32.Parse(value);
                        nrmList = new List<Vector3>(fccount);
                        faceList = new List<int[]>(fccount);
                        break;
                    case "normal":
                        GetLineData(value, lineSet, out lineIndex, out lineAxis, out fltVal);
                        switch (lineAxis)
                        {
                            case "X":
                                tempNrm = new Vector3(fltVal, 0, 0);
                                break;
                            case "Y":
                                tempNrm.Y = fltVal;
                                break;
                            case "Z":
                                tempNrm.Z = fltVal;
                                nrmList.Add(tempNrm);
                                break;
                        }
                        break;
                    case "uv":
                        GetLineData(value, lineSet, out lineIndex, out lineAxis, out fltVal);
                        switch (lineAxis)
                        {
                            case "U":
                                tempUv = new Vector2(fltVal, 0);
                                break;
                            case "V":
                                tempUv.Y = fltVal;
                                uvList.Add(tempUv);
                                break;
                        }
                        break;
                    case "vertex":
                        GetLineData(value, lineSet, out lineIndex, out lineAxis, out fltVal);
                        switch (lineAxis)
                        {
                            case "X":
                                tempPos = new Vector3(fltVal, 0, 0);
                                break;
                            case "Y":
                                tempPos.Y = fltVal;
                                break;
                            case "Z":
                                tempPos.Z = fltVal;
                                posList.Add(tempPos);
                                break;
                        }
                        break;
                    case "vertexnormal":
                        GetLineData(value, lineSet, out lineIndex, out lineAxis, out fltVal);
                        switch (lineAxis)
                        {
                            case "X":
                                tempVNrm = new Vector3(fltVal, 0, 0);
                                break;
                            case "Y":
                                tempVNrm.Y = fltVal;
                                break;
                            case "Z":
                                tempVNrm.Z = fltVal;
                                vertNrmList.Add(tempVNrm);
                                break;
                        }
                        break;
                    case "faceindx":
                        var idx = Int32.Parse(value);
                        GetLineData(value, lineSet, out lineIndex, out lineAxis, out fltVal);
                        switch (lineAxis)
                        {
                            case "0":
                                tempFace = new int[] { idx, 0, 0 };
                                break;
                            case "1":
                                tempFace[1] = idx;
                                break;
                            case "2":
                                tempFace[2] = idx;
                                faceList.Add(tempFace);
                                break;
                        }
                        break;
                }
            }
        }

        public List<string> GetWavefrontObj()
        {
            List<string> obj = new();
            foreach (var ver in posList)
            {
                obj.Add($"v {ver.X} {ver.Y} {ver.Z}");
            }
            foreach (var verNrm in nrmList)
            {
                var ver = -verNrm;
                obj.Add($"vn {ver.X} {ver.Y} {ver.Z}");
            }
            foreach (var ver in uvList)
            {
                obj.Add($"vt {ver.X} {ver.Y}");
            }
            for(int i = 0; i < faceList.Count; i++)
            {
                var face = faceList[i];
                obj.Add($"f {face[2] + 1}/{i + 1}/{i + 1} {face[1] + 1}/{i + 1}/{i + 1} {face[0] + 1}/{i + 1}/{i + 1}");
            }
            return obj;
        }

        private static void GetLineData(string value, string[] lineSet, out string lineIndex, out string lineAxis, out float fltVal)
        {
            lineIndex = lineSet[1].Split(']')[0];
            lineAxis = lineSet[2].Split(']')[0];
            fltVal = Single.Parse(value);
        }
    }
}
