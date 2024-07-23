using System.Collections;
using System.Collections.Generic;
using System.IO;
using AssetBundleFramework.Core.Resource;
using UnityEngine;

public class Test_Callback : MonoBehaviour
{
    private string PrefixPath { get; set; }//ǰ׺·��

    private string Platform { get; set; }//ƽ̨

    private void Start()
    {
        //��ƽ̨��ֵ
        Platform = GetPlatform();
        //��λ·��
        //Application ��Unity���ȷ�װ�õ�һ��·��
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
                throw new System.Exception($"��֧�ֵ�ƽ̨:{Application.platform}");

        }
    }

    private string GetFileUrl(string assetUrl)
    {
        return $"{PrefixPath}/{assetUrl}";
    }
}
