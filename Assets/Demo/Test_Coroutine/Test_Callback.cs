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

        Initialize();
    }

    /// <summary>
    /// ��ʼ��
    /// </summary>
    private void Initialize()
    {
        ResourceManager.instance.LoadWithCallback("Assets/AssetBundle/UI/UIRoot.prefab", false, //�Ƿ��첽����
            uiRootResource => 
            {
                uiRootResource.Instantiate();
                Transform uiParent = GameObject.Find("Canvas").transform;
                //�ټ���TestUI
                ResourceManager.instance.LoadWithCallback("Assets/AssetBundle/UI/TestUI.prefab", false, testUIResource =>
                {
                    testUIResource.Instantiate(uiParent, false);
                });
            });
    }

    private void Update()
    {
        ResourceManager.instance.Update();
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
