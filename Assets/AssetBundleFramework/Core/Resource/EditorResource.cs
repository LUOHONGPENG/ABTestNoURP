
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
    }

}
