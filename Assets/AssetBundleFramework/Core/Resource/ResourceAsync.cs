using UnityEngine;
using System;
using Object = UnityEngine.Object;
using AssetBundleFramework.Core.Bundle;

namespace AssetBundleFramework.Core.Resource
{
    internal class ResourceAsync : AResourceAsync
    {
        public override bool keepWaiting => !done;

        //�첽���Ի�Ƚϸ��� ��Ҫ������һ���첽������

        /// <summary>
        /// �첽������Դ��AssetBundleRequest
        /// </summary>
        private AssetBundleRequest m_AssetBundleRequest;

        /// <summary>
        /// ������Դ
        /// </summary>
        internal override void Load()
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException($"{nameof(Resource)}.{nameof(Load)}() {nameof(url)} is null.");
            }

            //bundleû��
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
        /// ж����Դ
        /// </summary>
        internal override void UnLoad()
        {
            if(bundle == null)
            {
                throw new Exception($"{nameof(Resource)}.{nameof(UnLoad)}(){nameof(bundle)} is null.");
            }

            if(base.asset !=null &&!(base.asset is GameObject))
            {
                Resources.UnloadAsset(base.asset);
                asset = null;
            }

            m_AssetBundleRequest = null;
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
                throw new Exception($"{nameof(ResourceAsync)}.{nameof(LoadAsset)}(){nameof(bundle)} is null.");
            }

            if (!bundle.isStreamedSceneAssetBundle)
            {
                //������û�м��ص�����
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

        /// <summary>
        /// �첽������Դ
        /// </summary>
        internal override void LoadAssetAsync()
        {
            if(bundle == null)
            {
                throw new Exception($"{nameof(ResourceAsync)}.{nameof(LoadAssetAsync)}(){nameof(bundle)} is null.");
            }

            m_AssetBundleRequest = bundle.LoadAssetAsync(url, typeof(Object));
        }


        /// <summary>
        /// ֡����
        /// </summary>
        /// <returns></returns>
        public override bool Update()
        {
            if (done)
            {
                return true;
            }

            if (dependencies != null)
            {
                for(int i = 0; i < dependencies.Length; i++)
                {
                    if (!dependencies[i].done)
                    {
                        return false;
                    }
                }
            }

            if (!bundle.done)
            {
                return false;
            }

            //����Ϊ�յĻ�
            if(m_AssetBundleRequest == null)
            {
                LoadAssetAsync();
            }

            if(m_AssetBundleRequest != null && !m_AssetBundleRequest.isDone)
            {
                return false;
            }

            LoadAsset();

            return true;
        }
    }
}

