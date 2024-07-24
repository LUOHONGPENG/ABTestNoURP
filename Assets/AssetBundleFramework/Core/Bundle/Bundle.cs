using System.IO;
using System;
using UnityEngine;
using Object = UnityEngine.Object;


namespace AssetBundleFramework.Core.Bundle
{
    internal class Bundle : ABundle
    {
        /// <summary>
        /// ����AssetBundle
        /// </summary>
        internal override void Load()
        {
            if (assetBundle)
            {
                throw new System.Exception($"{nameof(Bundle)}.{nameof(Load)}() {nameof(assetBundle)} not null, Url:{url}.");
            }

            string file = BundleManager.instance.GetFileUrl(url);

            //�����Editor�Ļ�
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
        /// ������Դ
        /// </summary>
        /// <param name="name">��Դ����</param>
        /// <param name="type">��ԴType</param>
        /// <returns>ָ�����ֵ���Դ</returns>
        internal override Object LoadAsset(string name, System.Type type)
        {
            //�ȼ�������Ƿ�Ϊ��
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"{nameof(Bundle)}.{nameof(LoadAsset)}() name is null.");
            }

            //assetbundle ��������ǿյ�ҲҪ����
            if(assetBundle == null)
            {
                throw new NullReferenceException($"{nameof(Bundle)}.{nameof(LoadAsset)}() Bundle is null");
            }

            //���ŷ��ؾ���AssetBundle�����ֵ
            return assetBundle.LoadAsset(name, type);
        }

        /// <summary>
        /// �첽������Դ
        /// </summary>
        /// <param name="name">��Դ����</param>
        /// <param name="type">��Դtype</param>
        /// <returns>AssetBundleRequest</returns>
        internal override AssetBundleRequest LoadAssetAsync(string name, Type type)
        {

        }
    }



}
