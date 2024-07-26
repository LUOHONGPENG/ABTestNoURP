
using AssetBundleFramework.Core.Bundle;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetBundleFramework.Core.Resource
{
    /// <summary>
    /// ����������
    /// </summary>
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
        /// ������Դ����(����֮��ͳ�פ������ֵ��� ����)
        /// </summary>
        private Dictionary<string, AResource> m_ResourceDic = new Dictionary<string, AResource>();

        /// <summary>
        /// ��Ҫ�ͷ�ж�ص���Դ����ʱ��ͳһ�ڷ�֡ж�أ�
        /// </summary>
        private LinkedList<AResource> m_NeedUnloadList = new LinkedList<AResource>();

        /// <summary>
        /// �첽���ؼ��ϣ�����ʱʲôʱ�������
        /// </summary>
        private List<AResourceAsync> m_AsyncList = new List<AResourceAsync>();

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
    

        /// <summary>
        /// ������Դ
        /// </summary>
        /// <param name="url">��Դurl</param>
        /// <param name="async">�Ƿ��첽</param>
        /// <param name="callback">������ɻص�</param>
        public void LoadWithCallback(string url,bool async,Action<IResource> callback)
        {
            //IResource��һ���ӿڻ���֮��ģ�
            AResource resource = LoadInternal(url, async, false);
            if (resource.done)
            {
                callback?.Invoke(resource);
            }
            else
            {
                resource.finishedCallback += callback;
            }
        }
    
        /// <summary>
        /// �ڲ�������Դ������������Դ�ģ�
        /// </summary>
        /// <param name="url">��Դurl</param>
        /// <param name="async">�Ƿ��첽</param>
        /// <param name="dependency">�Ƿ�����</param>
        /// <returns></returns>
        private AResource LoadInternal(string url,bool async,bool dependency)
        {
            AResource resource = null;
            //ȡresource
            if(m_ResourceDic.TryGetValue(url,out resource))
            {
                //����Ҫ�ͷŵ��б����Ƴ���Ϊʲô
                if(resource.reference == 0)//������Ϊ0
                {
                    m_NeedUnloadList.Remove(resource);
                    //û����

                    //����Resource������������
                }
                resource.AddReference();

                return resource;
            }

            //ûȡ���Ļ�


            if (m_Editor)
            {
                resource = new EditorResource();
            }
            else if (async)
            {
                ResourceAsync resourceAsync = new ResourceAsync();
                m_AsyncList.Add(resourceAsync);
                resource = resourceAsync;
            }
            else
            {
                resource = new Resource();
            }

            resource.url = url;
            m_ResourceDic.Add(url, resource);

            //��������
            List<string> dependencies = null;
            ResourceDependencyDic.TryGetValue(url, out dependencies);
            if (dependencies != null && dependencies.Count>0)
            {
                resource.dependencies = new AResource[dependencies.Count];
                for(int i = 0; i < dependencies.Count; i++)
                {
                    string dependencyUrl = dependencies[i];
                    AResource dependencyResource = LoadInternal(dependencyUrl, async, true);
                    resource.dependencies[i] = dependencyResource;
                }
            }
            resource.AddReference();
            resource.Load();
            return resource;

        }

        /// <summary>
        /// ж����Դ
        /// </summary>
        /// <param name="resource"></param>
        public void UnLoad(IResource resource)
        {
            if(resource == null)
            {
                throw new ArgumentNullException($"{nameof(ResourceManager)}.{nameof(UnLoad)}() {nameof(resource)} is null.");
            }

            AResource aResource = resource as AResource;
            aResource.ReduceReference();

            if(aResource.reference == 0)
            {
                WillUnload(aResource);
            }
        }

        /// <summary>
        /// ����Ҫ�ͷŵ���Դ
        /// </summary>
        /// <param name="resource"></param>
        private void WillUnload(AResource resource)
        {
            m_NeedUnloadList.AddLast(resource);
        }


        public void Update()
        {
            BundleManager.instance.Update();

            for(int i = 0; i < m_AsyncList.Count; i++)
            {
                AResourceAsync resourceAsync = m_AsyncList[i];
                if (resourceAsync.Update())
                {
                    m_AsyncList.RemoveAt(i);
                    i--;
                }
            }
        }

        public void LateUpdate()
        {
            if(m_NeedUnloadList.Count != 0)
            {
                //���ȡ
                while(m_NeedUnloadList.Count > 0)
                {
                    AResource resource = m_NeedUnloadList.First.Value;
                    m_NeedUnloadList.RemoveFirst();
                    //����������ݾ�continue
                    if(resource == null)
                    {
                        continue;
                    }

                    m_ResourceDic.Remove(resource.url);

                    resource.UnLoad();

                    //��������-1
                    if(resource.dependencies != null)
                    {
                        for(int i = 0; i < resource.dependencies.Length; i++)
                        {
                            AResource temp = resource.dependencies[i];
                            UnLoad(temp);
                        }
                    }
                }
            }

            BundleManager.instance.LateUpdate();
        }
    }
}
