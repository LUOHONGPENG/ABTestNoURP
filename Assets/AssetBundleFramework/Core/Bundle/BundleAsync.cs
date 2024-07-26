
using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;


namespace AssetBundleFramework.Core.Bundle
{
    /// <summary>
    /// �첽���ص�Bundle
    /// </summary>
    internal class BundleAsync : ABundleAsync
    {
        /// <summary>
        /// �첽bundle��AssetBundleCreateRequest
        /// </summary>
        private AssetBundleCreateRequest m_AssetBundleCreateRequest;

        /// <summary>
        /// ����AssetBundle
        /// </summary>
        internal override void Load()
        {
            if(m_AssetBundleCreateRequest != null)
            {
                throw new System.Exception($"{nameof(BundleAsync)}.{nameof(Load)}() " +
                    $"{nameof(m_AssetBundleCreateRequest)} not null,{this}.");
            }

            string file = BundleManager.instance.GetFileUrl(url);

            //�����Editor�Ļ�
            #if UNITY_EDITOR || UNITY_STANDALONE
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"{nameof(BundleAsync)}.{nameof(Load)}() {nameof(file)} not exist,file:{file}.");
            }
#endif

            m_AssetBundleCreateRequest = AssetBundle.LoadFromFileAsync(file, 0, BundleManager.instance.offset);
        }

        /// <summary>
        /// ж��bundle
        /// </summary>
        internal override void UnLoad()
        {
            if (assetBundle)
            {
                assetBundle.Unload(true);
            }
            else
            {
                //�����첽���ص���ԴҲҪ�е����߳̽����ͷ�
                if(m_AssetBundleCreateRequest != null)//�������Ҳ��Ϊ��
                {
                    //ȡ���첽���ص���Դ
                    assetBundle = m_AssetBundleCreateRequest.assetBundle;
                }

                //ȡ��֮�����ͷţ�
                if (assetBundle)
                {
                    assetBundle.Unload(true);
                }
            }
            m_AssetBundleCreateRequest = null;
            //���ֶ������ָ�һ��Ĭ��ֵ
            done = false;
            reference = 0;
            assetBundle = null;
            isStreamedSceneAssetBundle = false;
        }


        /// <summary>
        /// ������Դ
        /// </summary>
        /// <param name="name">��Դ����</param>
        /// <param name="type">��ԴType</param>
        /// <returns>ָ�����ֵ���Դ</returns>
        internal override Object LoadAsset(string name, System.Type type)
        {
            //�ȼ�������Ƿ�Ϊ��
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"{nameof(BundleAsync)}.{nameof(LoadAsset)}() name is null.");
            }

            //assetbundle ��������ǿյ�ҲҪ����
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
        /// �첽������Դ
        /// </summary>
        /// <param name="name">��Դ����</param>
        /// <param name="type">��Դtype</param>
        /// <returns>AssetBundleRequest ������һ��request</returns>
        internal override AssetBundleRequest LoadAssetAsync(string name, Type type)
        {
            //�ȼ�������Ƿ�Ϊ��
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"{nameof(BundleAsync)}.{nameof(LoadAssetAsync)}() name is null.");
            }

            //assetbundle ��������ǿյ�ҲҪ����
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

            //�������������
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

            //���Request����û
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

