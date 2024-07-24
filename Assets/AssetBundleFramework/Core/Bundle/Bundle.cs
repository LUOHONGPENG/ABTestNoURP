using System.IO;
using UnityEngine;

namespace AssetBundleFramework.Core.Bundle
{
    internal class Bundle : ABundle
    {
        /// <summary>
        /// 加载AssetBundle
        /// </summary>
        internal override void Load()
        {
            if (assetBundle)
            {
                throw new System.Exception($"{nameof(Bundle)}.{nameof(Load)}() {nameof(assetBundle)} not null, Url:{url}.");
            }

            string file = BundleManager.instance.GetFileUrl(url);

            //如果是Editor的话
            #if UNITY_EDITOR || UNITY_STANDALONE
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"{nameof(Bundle)}.{nameof(file)} not exist,file:{file}.");
            }
            #endif
            assetBundle = AssetBundle.LoadFromFile(file, 0, BundleManager.instance.offset);

            isStreamedSceneAssetBundle = assetBundle.isStreamedSceneAssetBundle;

            done = true;
        }
    }

}
