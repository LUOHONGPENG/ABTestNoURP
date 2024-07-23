using System.Collections;
using System.Collections.Generic;
using System.IO;
using AssetBundleFramework.Core.Resource;
using UnityEngine;

public class Test_Callback : MonoBehaviour
{
    private string PrefixPath { get; set; }//前缀路径

    private string Platform { get; set; }//平台

    private void Start()
    {
        //给平台赋值
        Platform = GetPlatform();
        //定位路径
        //Application 是Unity事先封装好的一个路径
        PrefixPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../AssetBundle")).Replace("\\", "/");
        PrefixPath += $"/{Platform}";
        ResourceManager.instance.Initialize(GetPlatform(), GetFileUrl, false, 0);
    }

    private string GetPlatform()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
                return "Windows";
            case RuntimePlatform.Android:
                return "Android";
            case RuntimePlatform.IPhonePlayer:
                return "iOS";
            default:
                throw new System.Exception($"不支持的平台:{Application.platform}");

        }
    }

    private string GetFileUrl(string assetUrl)
    {
        return $"{PrefixPath}/{assetUrl}";
    }
}
