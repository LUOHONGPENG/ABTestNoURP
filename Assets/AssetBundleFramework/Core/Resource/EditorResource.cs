
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetBundleFramework.Core.Resource
{
    /// <summary>
    /// 实际过程 不会所有资源都是用AB的形式加载的
    /// 编辑器下频繁打AB包很麻烦 
    /// 编辑器下的加载方式
    /// 这样方便一点
    /// </summary>
    internal class EditorResource : AResource
    {
        public override bool keepWaiting => !done;

        /// <summary>
        /// 加载资源
        /// </summary>
        internal override void Load()
        {
            //判断url在不在
            if(string.IsNullOrEmpty(url))
            {
                throw new ArgumentException($"{nameof(EditorResource)}.{nameof(url)} is null");
            }
            LoadAsset();
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        internal override void LoadAsset()
        {
            //如果是Editor可以直接拿到资源
            #if UNITY_EDITOR
                asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(url);
            #endif

            done = true;

            if(finishedCallback != null)
            {
                Action<AResource> tempCallback = finishedCallback;
                finishedCallback = null;
                tempCallback.Invoke(this);
            }
        }

        internal override async void UnLoad()
        {
            if(asset!=null &&!(asset is GameObject))
            {
                Resources.UnloadAsset(base.asset);
                asset = null;
            }
            //赋值
            asset = null;
            //awaiter = null;
            finishedCallback = null;
        }
    }

}
