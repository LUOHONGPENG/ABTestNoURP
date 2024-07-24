using AssetBundleFramework.Core.Bundle;
using System;
using UnityEngine;

namespace AssetBundleFramework.Core.Resource
{
    /// <summary>
    /// ��IResource�ӿڽ��н�һ��ʵ��
    /// </summary>
    internal abstract class AResource : CustomYieldInstruction, IResource
    {
        //CustomYieldInstruction�ǿ�����Э�̹�����߿����Ķ���

        /// <summary>
        /// Asset��Ӧ��Url
        /// </summary>
        public string url { get; set; }

        /// <summary>
        /// ������ɵ���Դ
        /// </summary>
        public virtual Object asset { get; protected set; }

        /// <summary>
        /// ���õ�Bundle
        /// </summary>
        internal ABundle bundle { get; set; }


        /// <summary>
        /// ������Դ
        /// </summary>
        internal AResource[] dependencies { get; set; }

        /// <summary>
        /// ���ü�����
        /// </summary>
        internal int reference { get; set; }

        /// <summary>
        /// ������ɻص�
        /// </summary>
        internal Action<AResource> finishedCallback { get; set; }

        public Object GetAsset()
        {
            return asset;
        }

        /// <summary>
        /// �Ƿ�������
        /// </summary>
        internal bool done { get; set; }

        /// <summary>
        /// ��������
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

            //��֤asset�Ǹ�GameObject�ű�ʵ����
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
        /// ������Դ
        /// </summary>
        internal abstract void Load();

        /// <summary>
        /// ������Դ
        /// </summary>
        internal abstract void LoadAsset();

        /// <summary>
        /// ˢ���첽��Դ����ͬ����Դ�������������첽�ǣ���Ҫ����ˢ�·��أ�
        /// </summary>
        internal void FreshAsyncAsset()
        {
            //���¾�return ����ݹ�
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

