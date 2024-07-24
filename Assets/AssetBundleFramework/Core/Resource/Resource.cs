
using AssetBundleFramework.Core.Bundle;
using System;

namespace AssetBundleFramework.Core.Resource
{
    internal class Resource : AResource
    {
        public override bool keepWaiting => !done;

        internal override void Load()
        {
            //先判断Url是否为空
            if(string.IsNullOrEmpty(url))
            {
                throw new Exception($"{nameof(Resource)}.{nameof(Load)}(){nameof(url)} is null.");
            }

            //bundle如果为空也是不对的
            if(bundle != null)
            {
                throw new Exception($"{nameof(Resource)}.{nameof(Load)}(){nameof(bundle)} not null.");
            }

            //去resourcemanager里取
            string bundleUrl = null;
            //如果resourcemanager也没有
            if(!ResourceManager.instance.ResourceBundleDic.TryGetValue(url,out bundleUrl))
            {
                throw new Exception($"{nameof(Resource)}.{nameof(Load)}(){nameof(bundleUrl)} is null.");
            }

            bundle = BundleManager.instance.Load(bundleUrl);
            LoadAsset();
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        internal override void LoadAsset()
        {
            if(bundle == null)
            {
                throw new System.Exception($"{nameof(Resource)}.{nameof(LoadAsset)}() {nameof(bundle)} is null.");
            }

            //正在异步加载的资源要变成同步
            FreshAsyncAsset();

            if (!bundle.isStreamedSceneAssetBundle)
            {
                asset = bundle.LoadAsset(url, typeof(Object));
            }

            asset = bundle.LoadAsset(url, typeof(Object));
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

