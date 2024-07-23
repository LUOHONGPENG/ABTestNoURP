
using AssetBundleFramework.Core.Bundle;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetBundleFramework.Core.Resource
{
    public class ResourceManager
    {
        /// <summary>
        /// ����
        /// </summary>
        public static ResourceManager instance { get; } = new ResourceManager();

        private const string MANIFEST_BUNDLE = "manifest.ab";
        //��ʱ����
        private const string RESOURCE_ASSET_NAME = "Assets/Temp/Resource.bytes";
        private const string BUNDLE_ASSET_NAME = "Assets/Temp/Bundle.bytes";
        private const string DEPENDENCY_ASSET_NAME = "Assets/Temp/Dependency.bytes";

        /// <summary>
        /// ������Դ��Ӧ��bundle
        /// </summary>
        internal Dictionary<string, string> ResourceBundleDic = new Dictionary<string, string>();

        /// <summary>
        /// ������Դ��������ϵ ���ܻ�����һ��
        /// </summary>
        internal Dictionary<string, List<string>> ResourceDependencyDic = new Dictionary<string, List<string>>();


        /// <summary>
        /// Ӱ���Ƿ�ʹ��AssetDataBase���м���(��ʼ��ʱ���¼)
        /// </summary>
        private bool m_Editor;

        /// <summary>
        /// ��ʼ�����������ӣ���ƽ̨�����ļ�·����������������ȥ������Ӧ��bundleֵȥ�����εĴ洢
        /// </summary>
        /// <param name="platform">ƽ̨</param>
        /// <param name="getFileCallback">��ȡ��Դ��ʵ·�� ��Ϊƽ̨��һ�� ��ȡbundle��·���ǲ�һ����</param>
        /// <param name="editor">�Ƿ�ʹ��AssetDataBase���� �Ƿ��Ǳ༭��ƽ̨</param>
        /// <param name="offset">��ȡbundle��ƫ�� </param>
        public void Initialize(string platform,Func<string,string> getFileCallback,bool editor,ulong offset)
        {
            m_Editor = editor;

            if (m_Editor)
            {
                return;
            }

            BundleManager.instance.Initialize(platform, getFileCallback, offset);

            string manifestBundleFile = getFileCallback.Invoke(MANIFEST_BUNDLE);
            AssetBundle manifestAssetBundle = AssetBundle.LoadFromFile(manifestBundleFile, 0, offset);

            //��ʱ����������Ҫ��ȡ������
            TextAsset resourceTextAsset = manifestAssetBundle.LoadAsset(RESOURCE_ASSET_NAME) as TextAsset;
            TextAsset bundleTextAsset = manifestAssetBundle.LoadAsset(BUNDLE_ASSET_NAME) as TextAsset;
            TextAsset dependencyTextAsset = manifestAssetBundle.LoadAsset(DEPENDENCY_ASSET_NAME) as TextAsset;

            byte[] resourceBytes = resourceTextAsset.bytes;
            byte[] bundleBytes = bundleTextAsset.bytes;
            byte[] dependencyBytes = dependencyTextAsset.bytes;

            manifestAssetBundle.Unload(true);
            manifestAssetBundle = null;

            //���ڱ���id��Ӧ��assetUrl
            Dictionary<ushort, string> assetUrlDic = new Dictionary<ushort, string>();

            //��ȡ��ԴĿ¼
            #region ��ȡ��Դ��Ϣ(Resource)
            {

                MemoryStream resourceMemoryStream = new MemoryStream(resourceBytes);
                BinaryReader resourceBinaryReader = new BinaryReader(resourceMemoryStream);
                //��ȡ��Դ����
                ushort resourceCount = resourceBinaryReader.ReadUInt16();
                for(ushort i = 0; i < resourceCount; i++)
                {
                    string assetUrl = resourceBinaryReader.ReadString();
                    assetUrlDic.Add(i, assetUrl);
                }
            }
            #endregion

            //��ȡBundle��Ϣ
            #region ��ȡbundle��Ϣ
            {
                ResourceBundleDic.Clear();

                MemoryStream bundleMemoryStream = new MemoryStream(bundleBytes);
                BinaryReader bundleBinaryReader = new BinaryReader(bundleMemoryStream);
                //��ȡBundle����
                ushort bundleCount = bundleBinaryReader.ReadUInt16();
                //����һ��
                for(int i = 0; i < bundleCount; i++)
                {
                    string bundleUrl = bundleBinaryReader.ReadString();
                    string bundleFileUrl = bundleUrl;
                    //�����ٶ�һ�������Ѽ�resҲ����asset bundle���������asset
                    ushort resourceCount = bundleBinaryReader.ReadUInt16();
                    for(int ii = 0; ii < resourceCount; ii++)
                    {
                        ushort assetId = bundleBinaryReader.ReadUInt16();
                        //���Id�ܺ�ǰ�漸��Resource��assetUrlDic��Ӧ��
                        string assetUrl = assetUrlDic[assetId];
                        ResourceBundleDic.Add(assetUrl, bundleFileUrl);
                    }
                }

            }
            #endregion

            #region ��ȡ��Դ������Ϣ
            {
                //����ʽ
                ResourceDependencyDic.Clear();
                MemoryStream dependencyMemoryStream = new MemoryStream(dependencyBytes);
                BinaryReader dependencyBinaryReader = new BinaryReader(dependencyMemoryStream);
                //��ȡ����������
                ushort dependencyCount = dependencyBinaryReader.ReadUInt16();
                for(int i = 0; i < dependencyCount; i++)
                {
                    //��ȡ��Դ����
                    ushort resourceCount = dependencyBinaryReader.ReadUInt16();
                    ushort assetId = dependencyBinaryReader.ReadUInt16();
                    string assetUrl = assetUrlDic[assetId];
                    List<string> dependencyList = new List<string>(resourceCount);
                    for(int ii = 1; ii < resourceCount; ii++)
                    {
                        ushort dependencyAssetId = dependencyBinaryReader.ReadUInt16();
                        string dependencyUrl = assetUrlDic[dependencyAssetId];
                        dependencyList.Add(dependencyUrl);
                    }
                    ResourceDependencyDic.Add(assetUrl,dependencyList);
                }
            }
            #endregion
        }
    }
}
