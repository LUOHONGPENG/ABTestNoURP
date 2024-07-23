
using System;
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

    }

}
