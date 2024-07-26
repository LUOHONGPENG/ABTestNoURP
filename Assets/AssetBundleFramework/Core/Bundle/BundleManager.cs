
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleFramework.Core.Bundle
{
    /// <summary>
    /// internal�ķ��ʼ����ǽ��޵�ǰ����
    /// </summary>
    internal class BundleManager
    {
        public readonly static BundleManager instance = new BundleManager();

        /// <summary>
        /// ����bundle��ʼ��ƫ��
        /// </summary>
        internal ulong offset { get; private set; }

        /// <summary>
        /// ��ȡ��Դ��ʵ·���ص�
        /// </summary>
        private Func<string, string> m_GetFileCallback;

        /// <summary>
        /// bundle����������Ϣ
        /// </summary>
        private AssetBundleManifest m_AssetBundleManifest;

        /// <summary>
        /// �����Ѽ��ص�bundle
        /// </summary>
        private Dictionary<string, ABundle> m_BundleDic = new Dictionary<string, ABundle>();

        /// <summary>
        /// �첽������bundle����ʱ�� ��Ҫ�ȱ��浽���б�
        /// </summary>
        private List<ABundleAsync> m_AsyncList = new List<ABundleAsync>();

        /// <summary>
        /// ��Ҫ�ͷŵ�bundle
        /// </summary>
        private LinkedList<ABundle> m_NeedUnloadList = new LinkedList<ABundle>();


        /// <summary>
        /// ��ʼ��
        /// </summary>
        /// <param name="platform">ƽ̨</param>
        /// <param name="getFileCallback">��ȡ��Դ��ʵ·���ص�</param>
        /// <param name="offset">����bundleƫ�� ����ʲô�������е���Դ������˽���Դ ��ֹ���ܵ� ��ʱΪ0����</param>
        internal void Initialize(string platform, Func<string,string> getFileCallback,ulong offset)
        {
            m_GetFileCallback = getFileCallback;
            this.offset = offset;

            //�����·�� ����plaform ����֪��manifest��·��
            string assetBundleManifestFile = getFileCallback.Invoke(platform);
            //���manifest�ļ�
            AssetBundle manifestAssetBundle = AssetBundle.LoadFromFile(assetBundleManifestFile);
            UnityEngine.Object[] objs = manifestAssetBundle.LoadAllAssets();
            if (objs.Length == 0) //Ϊ�����쳣
            {
                throw new Exception($"{nameof(BundleManager)}.{nameof(Initialize)}() AssetBundleManifest load fail");
            }

            m_AssetBundleManifest = objs[0] as AssetBundleManifest;
        }

        /// <summary>
        /// ͬ������
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal ABundle Load(string url)
        {
            return LoadInternal(url, false);
        }

        /// <summary>
        /// ж��bundle
        /// </summary>
        /// <param name="bundle">Ҫж�ص�bundle</param>
        internal void UnLoad(ABundle bundle)
        {
            if(bundle == null)
            {
                throw new ArgumentException($"{nameof(BundleManager)}.{nameof(UnLoad)}() bundle is null");
            }

            //���ü�һ
            bundle.ReduceReference();

            //����Ϊ0 ֱ���ͷ�
            if(bundle.reference == 0)
            {
                WillUnload(bundle);
            }
        }

        /// <summary>
        /// ����Ҫ�ͷŵ���Դ
        /// </summary>
        /// <param name="bundle"></param>
        private void WillUnload(ABundle bundle)
        {
            //���needunload
            m_NeedUnloadList.AddLast(bundle);
        }


        /// <summary>
        /// �첽����bundle
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal ABundle LoadAsync(string url)
        {
            return LoadInternal(url, true);
        }

        /// <summary>
        /// �ڲ�����bundle
        /// </summary>
        /// <param name="url">asset·��</param>
        /// <param name="async">�Ƿ��첽</param>
        /// <returns></returns>
        private ABundle LoadInternal(string url,bool async)
        {
            ABundle bundle;
            //�����Ѿ��е����
            if(m_BundleDic.TryGetValue(url,out bundle))
            {
                //Ϊ0 ����Ҫȥ����
                if(bundle.reference == 0)
                {
                    m_NeedUnloadList.Remove(bundle);
                }
                //�ӻ�����ȡ������+1
                bundle.AddReference();
                return bundle;
            }

            //����ab
            if (async)
            {
                //������첽�Ļ�
                bundle = new BundleAsync();
                bundle.url = url;
                m_AsyncList.Add(bundle as ABundleAsync);
            }
            else
            {
                bundle = new Bundle();
                bundle.url = url;
            }

            m_BundleDic.Add(url, bundle);

            //��������
            string[] dependencies = m_AssetBundleManifest.GetDirectDependencies(url);
            if(dependencies.Length > 0)
            {
                //����0 �Ļ� ���еݹ鴦��
                bundle.dependencies = new ABundle[dependencies.Length];
                for(int i = 0; i < dependencies.Length; i++)
                {
                    string dependencyUrl = dependencies[i];
                    ABundle dependencyBundle = LoadInternal(dependencyUrl, async);//�ݹ飡
                    bundle.dependencies[i] = dependencyBundle;
                }
            }

            bundle.AddReference();
            bundle.Load();//���¿���load��
            return bundle;
        }

        /// <summary>
        /// ��ȡbundle�ľ���·��
        /// </summary>
        /// <param name="url">url</param>
        /// <returns>bundle�ľ���·��</returns>
        internal string GetFileUrl(string url)
        {
            if(m_GetFileCallback == null)
            {
                throw new Exception($"{nameof(BundleManager)}.{nameof(GetFileUrl)}() {nameof(m_GetFileCallback)} is null.");
            }

            //�����ⲿ����
            return m_GetFileCallback.Invoke(url);
        }

        public void Update()
        {
            for(int i = 0; i < m_AsyncList.Count;i++)
            {
                if (m_AsyncList[i].Update())
                {
                    m_AsyncList.RemoveAt(i);
                    i--;
                }
            }
        }

        //��Update����֮�����
        public void LateUpdate()
        {
            if(m_NeedUnloadList.Count == 0)
            {
                return;
            }
            
            while(m_NeedUnloadList.Count > 0)
            {
                ABundle bundle = m_NeedUnloadList.First.Value;//���õ�ͷ
                m_NeedUnloadList.RemoveFirst();
                if(bundle == null)
                {
                    continue;
                }

                m_BundleDic.Remove(bundle.url);

                
                if(!bundle.done && bundle is BundleAsync)
                {
                    BundleAsync bundleAsync = bundle as BundleAsync;
                    //�첽���б����� ���Ƴ���
                    if (m_AsyncList.Contains(bundleAsync))
                    {
                        m_AsyncList.Remove(bundleAsync);
                    }
                }

                bundle.UnLoad();

                //������������-1
                if(bundle.dependencies != null)
                {
                    for(int i = 0; i < bundle.dependencies.Length; i++)
                    {
                        //ȡ������
                        ABundle temp = bundle.dependencies[i];
                        UnLoad(temp);
                    }
                }
            }
        }
    }

}
