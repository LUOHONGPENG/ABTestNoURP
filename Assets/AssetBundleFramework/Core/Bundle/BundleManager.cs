
using System;
using System.Collections.Generic;
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
        /// 所有已加载的bundle
        /// </summary>
        private Dictionary<string, ABundle> m_BundleDic = new Dictionary<string, ABundle>();

        /// <summary>
        /// 异步创建的bundle加载时候 需要先保存到该列表
        /// </summary>
        private List<ABundleAsync> m_AsyncList = new List<ABundleAsync>();

        /// <summary>
        /// 需要释放的bundle
        /// </summary>
        private LinkedList<ABundle> m_NeedUnloadList = new LinkedList<ABundle>();


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

        /// <summary>
        /// 同步加载
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal ABundle Load(string url)
        {
            return LoadInternal(url, false);
        }

        /// <summary>
        /// 异步加载bundle
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal ABundle LoadAsync(string url)
        {
            return LoadInternal(url, true);
        }

        /// <summary>
        /// 内部加载bundle
        /// </summary>
        /// <param name="url">asset路径</param>
        /// <param name="async">是否异步</param>
        /// <returns></returns>
        private ABundle LoadInternal(string url,bool async)
        {
            ABundle bundle;
            //这是已经有的情况
            if(m_BundleDic.TryGetValue(url,out bundle))
            {
                //为0 则需要去加载
                if(bundle.reference == 0)
                {
                    m_NeedUnloadList.Remove(bundle);
                }
                //从缓存中取并引用+1
                bundle.AddReference();
                return bundle;
            }

            //创建ab
            if (async)
            {
                //如果是异步的话
                bundle = new BundleAsync();
                bundle.url = url;
                m_AsyncList.Add(bundle as ABundleAsync);
            }
            else
            {
                bundle = new Bundle();
                bundle.url = url;
            }

            m_BundleDic.Add(url, bundle);

            //加载依赖
            string[] dependencies = m_AssetBundleManifest.GetDirectDependencies(url);
            if(dependencies.Length > 0)
            {
                //大于0 的话 进行递归处理
                bundle.dependencies = new ABundle[dependencies.Length];
                for(int i = 0; i < dependencies.Length; i++)
                {
                    string dependencyUrl = dependencies[i];
                    ABundle dependencyBundle = LoadInternal(dependencyUrl, async);//递归！
                    bundle.dependencies[i] = dependencyBundle;
                }
            }

            bundle.AddReference();
            bundle.Load();//完事可以load了
            return bundle;
        }

        /// <summary>
        /// 获取bundle的绝对路径
        /// </summary>
        /// <param name="url">url</param>
        /// <returns>bundle的绝对路径</returns>
        internal string GetFileUrl(string url)
        {
            if(m_GetFileCallback == null)
            {
                throw new Exception($"{nameof(BundleManager)}.{nameof(GetFileUrl)}() {nameof(m_GetFileCallback)} is null.");
            }

            //交到外部处理
            return m_GetFileCallback.Invoke(url);
        }
    }

}
