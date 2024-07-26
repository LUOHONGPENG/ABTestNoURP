
using AssetBundleFramework.Core.Bundle;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetBundleFramework.Core.Resource
{
    /// <summary>
    /// 管理具体加载
    /// </summary>
    public class ResourceManager
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static ResourceManager instance { get; } = new ResourceManager();

        private const string MANIFEST_BUNDLE = "manifest.ab";
        //临时名字
        private const string RESOURCE_ASSET_NAME = "Assets/Temp/Resource.bytes";
        private const string BUNDLE_ASSET_NAME = "Assets/Temp/Bundle.bytes";
        private const string DEPENDENCY_ASSET_NAME = "Assets/Temp/Dependency.bytes";

        /// <summary>
        /// 保存资源对应的bundle
        /// </summary>
        internal Dictionary<string, string> ResourceBundleDic = new Dictionary<string, string>();

        /// <summary>
        /// 保存资源的依赖关系 可能会依赖一大串
        /// </summary>
        internal Dictionary<string, List<string>> ResourceDependencyDic = new Dictionary<string, List<string>>();

        /// <summary>
        /// 所有资源集合(加载之后就常驻在这个字典里 缓存)
        /// </summary>
        private Dictionary<string, AResource> m_ResourceDic = new Dictionary<string, AResource>();

        /// <summary>
        /// 需要释放卸载的资源（到时候统一在分帧卸载）
        /// </summary>
        private LinkedList<AResource> m_NeedUnloadList = new LinkedList<AResource>();

        /// <summary>
        /// 异步加载集合（不定时什么时候回来）
        /// </summary>
        private List<AResourceAsync> m_AsyncList = new List<AResourceAsync>();

        /// <summary>
        /// 影响是否使用AssetDataBase进行加载(初始化时候记录)
        /// </summary>
        private bool m_Editor;

        /// <summary>
        /// 初始化函数，复杂，把平台还有文件路径都传给它，才能去解析相应的bundle值去做二次的存储
        /// </summary>
        /// <param name="platform">平台</param>
        /// <param name="getFileCallback">获取资源真实路径 因为平台不一样 获取bundle的路径是不一样的</param>
        /// <param name="editor">是否使用AssetDataBase加载 是否是编辑器平台</param>
        /// <param name="offset">获取bundle的偏移 </param>
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

            //临时的依赖表需要读取来解析
            TextAsset resourceTextAsset = manifestAssetBundle.LoadAsset(RESOURCE_ASSET_NAME) as TextAsset;
            TextAsset bundleTextAsset = manifestAssetBundle.LoadAsset(BUNDLE_ASSET_NAME) as TextAsset;
            TextAsset dependencyTextAsset = manifestAssetBundle.LoadAsset(DEPENDENCY_ASSET_NAME) as TextAsset;

            byte[] resourceBytes = resourceTextAsset.bytes;
            byte[] bundleBytes = bundleTextAsset.bytes;
            byte[] dependencyBytes = dependencyTextAsset.bytes;

            manifestAssetBundle.Unload(true);
            manifestAssetBundle = null;

            //用于保存id对应的assetUrl
            Dictionary<ushort, string> assetUrlDic = new Dictionary<ushort, string>();

            //读取资源目录
            #region 读取资源信息(Resource)
            {

                MemoryStream resourceMemoryStream = new MemoryStream(resourceBytes);
                BinaryReader resourceBinaryReader = new BinaryReader(resourceMemoryStream);
                //获取资源个数
                ushort resourceCount = resourceBinaryReader.ReadUInt16();
                for(ushort i = 0; i < resourceCount; i++)
                {
                    string assetUrl = resourceBinaryReader.ReadString();
                    assetUrlDic.Add(i, assetUrl);
                }
            }
            #endregion

            //读取Bundle信息
            #region 读取bundle信息
            {
                ResourceBundleDic.Clear();

                MemoryStream bundleMemoryStream = new MemoryStream(bundleBytes);
                BinaryReader bundleBinaryReader = new BinaryReader(bundleMemoryStream);
                //获取Bundle个数
                ushort bundleCount = bundleBinaryReader.ReadUInt16();
                //遍历一下
                for(int i = 0; i < bundleCount; i++)
                {
                    string bundleUrl = bundleBinaryReader.ReadString();
                    string bundleFileUrl = bundleUrl;
                    //二次再读一下用于搜集res也就是asset bundle里面包含着asset
                    ushort resourceCount = bundleBinaryReader.ReadUInt16();
                    for(int ii = 0; ii < resourceCount; ii++)
                    {
                        ushort assetId = bundleBinaryReader.ReadUInt16();
                        //这个Id能和前面几行Resource的assetUrlDic对应上
                        string assetUrl = assetUrlDic[assetId];
                        ResourceBundleDic.Add(assetUrl, bundleFileUrl);
                    }
                }

            }
            #endregion

            #region 读取资源依赖信息
            {
                //起手式
                ResourceDependencyDic.Clear();
                MemoryStream dependencyMemoryStream = new MemoryStream(dependencyBytes);
                BinaryReader dependencyBinaryReader = new BinaryReader(dependencyMemoryStream);
                //获取依赖链个数
                ushort dependencyCount = dependencyBinaryReader.ReadUInt16();
                for(int i = 0; i < dependencyCount; i++)
                {
                    //获取资源个数
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
        /// 加载资源
        /// </summary>
        /// <param name="url">资源url</param>
        /// <param name="async">是否异步</param>
        /// <param name="callback">加载完成回调</param>
        public void LoadWithCallback(string url,bool async,Action<IResource> callback)
        {
            //IResource是一个接口基类之类的？
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
        /// 内部加载资源（真正加载资源的）
        /// </summary>
        /// <param name="url">资源url</param>
        /// <param name="async">是否异步</param>
        /// <param name="dependency">是否依赖</param>
        /// <returns></returns>
        private AResource LoadInternal(string url,bool async,bool dependency)
        {
            AResource resource = null;
            //取resource
            if(m_ResourceDic.TryGetValue(url,out resource))
            {
                //从需要释放的列表中移除（为什么
                if(resource.reference == 0)//引用数为0
                {
                    m_NeedUnloadList.Remove(resource);
                    //没听懂

                    //经典Resource管理方案··？
                }
                resource.AddReference();

                return resource;
            }

            //没取到的话


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

            //加载依赖
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
        /// 卸载资源
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
        /// 即将要释放的资源
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
                //疯狂取
                while(m_NeedUnloadList.Count > 0)
                {
                    AResource resource = m_NeedUnloadList.First.Value;
                    m_NeedUnloadList.RemoveFirst();
                    //如果是脏数据就continue
                    if(resource == null)
                    {
                        continue;
                    }

                    m_ResourceDic.Remove(resource.url);

                    resource.UnLoad();

                    //依赖引用-1
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
