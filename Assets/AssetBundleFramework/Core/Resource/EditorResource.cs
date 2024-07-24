
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetBundleFramework.Core.Resource
{
    /// <summary>
    /// ʵ�ʹ��� ����������Դ������AB����ʽ���ص�
    /// �༭����Ƶ����AB�����鷳 
    /// �༭���µļ��ط�ʽ
    /// ��������һ��
    /// </summary>
    internal class EditorResource : AResource
    {
        public override bool keepWaiting => !done;

        /// <summary>
        /// ������Դ
        /// </summary>
        internal override void Load()
        {
            //�ж�url�ڲ���
            if(string.IsNullOrEmpty(url))
            {
                throw new ArgumentException($"{nameof(EditorResource)}.{nameof(url)} is null");
            }
            LoadAsset();
        }

        /// <summary>
        /// ������Դ
        /// </summary>
        internal override void LoadAsset()
        {
            //�����Editor����ֱ���õ���Դ
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
    }

}
