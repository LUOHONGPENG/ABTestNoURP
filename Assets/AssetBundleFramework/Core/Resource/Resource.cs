
using AssetBundleFramework.Core.Bundle;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetBundleFramework.Core.Resource
{
    internal class Resource : AResource
    {
        public override bool keepWaiting => !done;

        internal override void Load()
        {
            //���ж�Url�Ƿ�Ϊ��
            if(string.IsNullOrEmpty(url))
            {
                throw new Exception($"{nameof(Resource)}.{nameof(Load)}(){nameof(url)} is null.");
            }

            //bundle���Ϊ��Ҳ�ǲ��Ե�
            if(bundle != null)
            {
                throw new Exception($"{nameof(Resource)}.{nameof(Load)}(){nameof(bundle)} not null.");
            }

            //ȥresourcemanager��ȡ
            string bundleUrl = null;
            //���resourcemanagerҲû��
            if(!ResourceManager.instance.ResourceBundleDic.TryGetValue(url,out bundleUrl))
            {
                throw new Exception($"{nameof(Resource)}.{nameof(Load)}(){nameof(bundleUrl)} is null.");
            }

            bundle = BundleManager.instance.Load(bundleUrl);
            LoadAsset();
        }

        /// <summary>
        /// ж����Դ
        /// </summary>
        internal override void UnLoad()
        {
            if(bundle == null)
            {
                throw new Exception($"{nameof(Resource)}.{nameof(UnLoad)}(){nameof(bundle)} is null.");
            }

            if(asset != null &&!(asset is GameObject))
            {
                Resources.UnloadAsset(asset);
                asset = null;
            }

            BundleManager.instance.UnLoad(bundle);

            bundle = null;
            finishedCallback = null;
        }


        /// <summary>
        /// ������Դ
        /// </summary>
        internal override void LoadAsset()
        {
            if(bundle == null)
            {
                throw new System.Exception($"{nameof(Resource)}.{nameof(LoadAsset)}() {nameof(bundle)} is null.");
            }

            //�����첽���ص���ԴҪ���ͬ��
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

