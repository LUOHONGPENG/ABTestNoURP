
using System.IO;
using UnityEngine;

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


    }

}

