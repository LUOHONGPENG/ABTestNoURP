using UnityEngine;
using System;
using Object = UnityEngine.Object;
using AssetBundleFramework.Core.Bundle;

namespace AssetBundleFramework.Core.Resource
{
    internal class ResourceAsync : AResourceAsync
    {
        public override bool keepWaiting => !done;

        //异步所以会比较复杂 需要先声明一个异步的请求

        /// <summary>
        /// 异步加载资源的AssetBundleRequest
        /// </summary>
        private AssetBundleRequest m_AssetBundleRequest;

        /// <summary>
        /// 加载资源
        /// </summary>
        internal override void Load()
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException($"{nameof(Resource)}.{nameof(Load)}() {nameof(url)} is null.");
            }

            //bundle没有
            if(bundle != null)
            {
                throw new Exception($"{nameof(Resource)}.{nameof(Load)}() {nameof(bundle)} not null.");
            }

            string bundleUrl = null;
            if(!ResourceManager.instance.ResourceBundleDic.TryGetValue(url,out bundleUrl))
            {
                throw new Exception($"{nameof(ResourceAsync)}.{nameof(Load)}() {nameof(bundleUrl)} is null.");
            }

            bundle = BundleManager.instance.LoadAsync(bundleUrl);
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        internal override void LoadAsset()
        {
            if(bundle == null)
            {
                throw new Exception($"{nameof(ResourceAsync)}.{nameof(LoadAsset)}(){nameof(bundle)} is null.");
            }

            if (!bundle.isStreamedSceneAssetBundle)
            {
                //看看有没有加载的请求
                if(m_AssetBundleRequest != null)
                {
                    asset = m_AssetBundleRequest.asset;
                }
                else
                {
                    asset = bundle.LoadAsset(url, typeof(Object));
                }
            }

            done = true;

            if(finishedCallback != null)
            {
                Action<AResource> tempCallback = finishedCallback;
                finishedCallback = null;
                tempCallback.Invoke(this);

            }
        }
    }
}

