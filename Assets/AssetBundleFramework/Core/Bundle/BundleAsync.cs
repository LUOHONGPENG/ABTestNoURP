
using System.IO;
using UnityEngine;

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


    }

}

