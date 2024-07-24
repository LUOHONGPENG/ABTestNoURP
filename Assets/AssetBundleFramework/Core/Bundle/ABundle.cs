using System;
using UnityEngine;

namespace AssetBundleFramework.Core.Bundle
{
    public class ABundle
    {
        /// <summary>
        /// AssetBundle
        /// </summary>
        internal AssetBundle assetBundle { get; set; }

        /// <summary>
        /// bundle url
        /// </summary>
        internal string url { get; set; }

        /// <summary>
        /// 引用计数器
        /// </summary>
        internal int reference { get; set; }

        /// <summary>
        /// bundle是否加载完成
        /// </summary>
        internal bool done { get; set; }

        /// <summary>
        /// bundle 依赖
        /// </summary>
        internal ABundle[] dependencies { get; set; }

        /// <summary>
        /// 是否是场景
        /// </summary>
        internal bool isStreamedSceneAssetBundle { get; set; }

        /// <summary>
        /// 加载bundle
        /// </summary>
        internal abstract void Load();

        internal void AddReference()
        {
            //自身引用+1
            ++reference;
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="name">资源名称</param>
        /// <param name="type">资源Type</param>
        /// <returns>指定名字的资源</returns>
        internal abstract Object LoadAsset(string name, Type type);
    }

}
