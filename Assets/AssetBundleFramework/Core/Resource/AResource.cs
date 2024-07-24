using AssetBundleFramework.Core.Bundle;
using System;
using UnityEngine;

namespace AssetBundleFramework.Core.Resource
{
    /// <summary>
    /// 对IResource接口进行进一步实现
    /// </summary>
    internal abstract class AResource : CustomYieldInstruction, IResource
    {
        //CustomYieldInstruction是可以让协程挂起或者开启的东西

        /// <summary>
        /// Asset对应的Url
        /// </summary>
        public string url { get; set; }

        /// <summary>
        /// 加载完成的资源
        /// </summary>
        public virtual Object asset { get; protected set; }

        /// <summary>
        /// 引用的Bundle
        /// </summary>
        internal ABundle bundle { get; set; }


        /// <summary>
        /// 依赖资源
        /// </summary>
        internal AResource[] dependencies { get; set; }

        /// <summary>
        /// 引用计数器
        /// </summary>
        internal int reference { get; set; }

        /// <summary>
        /// 加载完成回调
        /// </summary>
        internal Action<AResource> finishedCallback { get; set; }

        public Object GetAsset()
        {
            return asset;
        }

        /// <summary>
        /// 是否加载完成
        /// </summary>
        internal bool done { get; set; }

        /// <summary>
        /// 增加引用
        /// </summary>
        internal void AddReference()
        {
            ++reference;
        }

        public GameObject Instantiate()
        {
            UnityEngine.Object obj = asset;

            if (!obj)
            {
                return null;
            }

            if(!(obj is GameObject))
            {
                return null;
            }

            //保证asset是个GameObject才被实例化
            return UnityEngine.Object.Instantiate(obj) as GameObject;
        }

        public GameObject Instantiate(Transform parent, bool instantiateInWorldSpace)
        {
            UnityEngine.Object obj = asset;

            if (!obj)
            {
                return null;
            }

            if (!(obj is GameObject))
            {
                return null;
            }

            return UnityEngine.Object.Instantiate(obj, parent, instantiateInWorldSpace) as GameObject;
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        internal abstract void Load();

        /// <summary>
        /// 加载资源
        /// </summary>
        internal abstract void LoadAsset();

        /// <summary>
        /// 刷新异步资源（当同步资源的依赖包包含异步是，需要立刻刷新返回）
        /// </summary>
        internal void FreshAsyncAsset()
        {
            //完事就return 否则递归
            if (done)
                return;

            if(dependencies != null)
            {
                for(int i = 0; i < dependencies.Length; i++)
                {
                    AResource resource = dependencies[i];
                    resource.FreshAsyncAsset();
                }
            }

            if(this is AResourceAsync)
            {
                LoadAsset();
            }
        }
    }
}

