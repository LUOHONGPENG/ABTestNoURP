using System.IO;
using System;
using UnityEngine;
using Object = UnityEngine.Object;


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

        /// <summary>
        /// 卸载bundle
        /// </summary>
        internal override void UnLoad()
        {
            if (assetBundle)
            {
                assetBundle.Unload(true);
            }

            //设定回默认值
            assetBundle = null;
            done = false;
            reference = 0;
            isStreamedSceneAssetBundle = false;
        }


        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="type">资源Type</param>
        /// <returns>AssetBundleRequest</returns>
        internal override AssetBundleRequest LoadAssetAsync(string name, Type type)
        {
            //先检查名字是否为空
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"{nameof(Bundle)}.{nameof(LoadAssetAsync)}() name is null.");
            }

            //assetbundle 本身如果是空的也要报错
            if (assetBundle == null)
            {
                throw new NullReferenceException($"{nameof(Bundle)}.{nameof(LoadAssetAsync)}() Bundle is null");
            }

            return assetBundle.LoadAssetAsync(name, type);
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
                throw new ArgumentException($"{nameof(Bundle)}.{nameof(LoadAsset)}() name is null.");
            }

            //assetbundle 本身如果是空的也要报错
            if(assetBundle == null)
            {
                throw new NullReferenceException($"{nameof(Bundle)}.{nameof(LoadAsset)}() Bundle is null");
            }

            //最后才返回具体AssetBundle里面的值
            return assetBundle.LoadAsset(name, type);
        }

        
    }



}
