namespace AssetBundleFramework.Core.Resource
{
    internal abstract class AResourceAsync : AResource
    {
        public abstract bool Update();

        /// <summary>
        /// �첽������Դ
        /// </summary>
        internal abstract void LoadAssetAsync();
    }


}
