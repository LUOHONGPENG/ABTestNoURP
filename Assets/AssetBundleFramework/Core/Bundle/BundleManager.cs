
using System;
using UnityEngine;

namespace AssetBundleFramework.Core.Bundle
{
    /// <summary>
    /// internal的访问级别是仅限当前程序集
    /// </summary>
    internal class BundleManager
    {
        public readonly static BundleManager instance = new BundleManager();

        /// <summary>
        /// 加载bundle开始的偏移
        /// </summary>
        internal ulong offset { get; private set; }

        /// <summary>
        /// 获取资源真实路径回调
        /// </summary>
        private Func<string, string> m_GetFileCallback;

        /// <summary>
        /// bundle依赖管理信息
        /// </summary>
        private AssetBundleManifest m_AssetBundleManifest;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="platform">平台</param>
        /// <param name="getFileCallback">获取资源真实路径回调</param>
        /// <param name="offset">加载bundle偏移 这是什么？就是有的资源不想别人解资源 防止解密的 暂时为0就行</param>
        internal void Initialize(string platform, Func<string,string> getFileCallback,ulong offset)
        {
            m_GetFileCallback = getFileCallback;
            this.offset = offset;

            //解相关路径 传入plaform 可以知道manifest的路径
            string assetBundleManifestFile = getFileCallback.Invoke(platform);
            //获得manifest文件
            AssetBundle manifestAssetBundle = AssetBundle.LoadFromFile(assetBundleManifestFile);
            UnityEngine.Object[] objs = manifestAssetBundle.LoadAllAssets();
            if (objs.Length == 0) //为零则异常
            {
                throw new Exception($"{nameof(BundleManager)}.{nameof(Initialize)}() AssetBundleManifest load fail");
            }

            m_AssetBundleManifest = objs[0] as AssetBundleManifest;

        }

    }

}
