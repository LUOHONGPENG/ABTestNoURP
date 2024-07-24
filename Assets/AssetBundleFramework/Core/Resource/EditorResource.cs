
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
    }

}
