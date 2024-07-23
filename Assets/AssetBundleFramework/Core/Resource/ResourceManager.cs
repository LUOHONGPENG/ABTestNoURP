
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
    }
}
