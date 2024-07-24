using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetBundleFramework.Core.Bundle
{
    public abstract class ABundle
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
        /// ���ü�����
        /// </summary>
        internal int reference { get; set; }

        /// <summary>
        /// bundle�Ƿ�������
        /// </summary>
        internal bool done { get; set; }

        /// <summary>
        /// bundle ����
        /// </summary>
        internal ABundle[] dependencies { get; set; }

        /// <summary>
        /// �Ƿ��ǳ���
        /// </summary>
        internal bool isStreamedSceneAssetBundle { get; set; }

        /// <summary>
        /// ����bundle
        /// </summary>
        internal abstract void Load();

        internal void AddReference()
        {
            //��������+1
            ++reference;
        }

        /// <summary>
        /// ������Դ
        /// </summary>
        /// <param name="name">��Դ����</param>
        /// <param name="type">��ԴType</param>
        /// <returns>ָ�����ֵ���Դ</returns>
        internal abstract Object LoadAsset(string name, Type type);

        /// <summary>
        /// �첽������Դ
        /// </summary>
        /// <param name="name">��Դ����</param>
        /// <param name="type">��Դtype</param>
        /// <returns>AssetBundleRequest</returns>
        internal abstract AssetBundleRequest LoadAssetAsync(string name, Type type);
    }

}
