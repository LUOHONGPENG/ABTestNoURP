
using UnityEngine;

namespace AssetBundleFramework.Core.Resource
{
    public interface IResource
    {
        Object GetAsset();
        GameObject Instantiate();
        GameObject Instantiate(Transform parent, bool instantiateInWorldSpace);
    }
}


