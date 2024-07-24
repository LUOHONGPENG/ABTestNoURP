namespace AssetBundleFramework.Core.Resource
{
    internal class ResourceAsync : AResourceAsync
    {
        public override bool keepWaiting => !done;
    }
}

