
using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;


namespace AssetBundleFramework.Core.Bundle
{
    /// <summary>
    /// 异步加载的Bundle
    /// </summary>
    internal class BundleAsync : ABundleAsync
    {
        /// <summary>
        /// 异步bundle的AssetBundleCreateRequest
        /// </summary>
        private AssetBundleCreateRequest m_AssetBundleCreateRequest;

        /// <summary>
        /// 加载AssetBundle
        /// </summary>
        internal override void Load()
        {
            if(m_AssetBundleCreateRequest != null)
            {
                throw new System.Exception($"{nameof(BundleAsync)}.{nameof(Load)}() " +
                    $"{nameof(m_AssetBundleCreateRequest)} not null,{this}.");
            }

            string file = BundleManager.instance.GetFileUrl(url);

            //如果是Editor的话
            #if UNITY_EDITOR || UNITY_STANDALONE
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"{nameof(BundleAsync)}.{nameof(Load)}() {nameof(file)} not exist,file:{file}.");
            }
#endif

            m_AssetBundleCreateRequest = AssetBundle.LoadFromFileAsync(file, 0, BundleManager.instance.offset);
        }

        /// <summary>
        /// 卸载bundle
        /// </summary>
        internal override void UnLoad()
        {
            if (assetBundle)
            {
                assetBundle.Unload(true);
            }
            else
            {
                //正在异步加载的资源也要切到主线程进行释放
                if(m_AssetBundleCreateRequest != null)//如果请求也不为空
                {
                    //取到异步加载的资源
                    assetBundle = m_AssetBundleCreateRequest.assetBundle;
                }

                //取到之后再释放！
                if (assetBundle)
                {
                    assetBundle.Unload(true);
                }
            }
            m_AssetBundleCreateRequest = null;
            //各种东西都恢复一下默认值
            done = false;
            reference = 0;
            assetBundle = null;
            isStreamedSceneAssetBundle = false;
        }


        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="type">资源Type</param>
        /// <returns>指定名字的资源</returns>
        internal override Object LoadAsset(string name, System.Type type)
        {
            //先检查名字是否为空
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"{nameof(BundleAsync)}.{nameof(LoadAsset)}() name is null.");
            }

            //assetbundle 本身如果是空的也要报错
            if (m_AssetBundleCreateRequest == null)
            {
                throw new NullReferenceException($"{nameof(BundleAsync)}.{nameof(LoadAsset)}() m_AssetBundleCreateRequest is null");
            }

            if(assetBundle == null)
            {
                assetBundle = m_AssetBundleCreateRequest.assetBundle;
            }

            return assetBundle.LoadAsset(name, type);
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="type">资源type</param>
        /// <returns>AssetBundleRequest 返回是一个request</returns>
        internal override AssetBundleRequest LoadAssetAsync(string name, Type type)
        {
            //先检查名字是否为空
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"{nameof(BundleAsync)}.{nameof(LoadAssetAsync)}() name is null.");
            }

            //assetbundle 本身如果是空的也要报错
            if (m_AssetBundleCreateRequest == null)
            {
                throw new NullReferenceException($"{nameof(BundleAsync)}.{nameof(LoadAssetAsync)}() m_AssetBundleCreateRequest is null");
            }

            if (assetBundle == null)
            {
                assetBundle = m_AssetBundleCreateRequest.assetBundle;
            }

            return assetBundle.LoadAssetAsync(name, type);
        }

        internal override bool Update()
        {
            if (done)
            {
                return true;
            }

            //检查依赖好了吗
            if(dependencies != null)
            {
                for(int i = 0; i < dependencies.Length; i++)
                {
                    if (!dependencies[i].done)
                    {
                        return false;
                    }
                }
            }

            //检查Request好了没
            if (!m_AssetBundleCreateRequest.isDone)
            {
                return false;
            }

            done = true;

            assetBundle = m_AssetBundleCreateRequest.assetBundle;
            isStreamedSceneAssetBundle = assetBundle.isStreamedSceneAssetBundle;

            if(reference == 0)
            {
                UnLoad();
            }

            return true;
        }

    }

}

