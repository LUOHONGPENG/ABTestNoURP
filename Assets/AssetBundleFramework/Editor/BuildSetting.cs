using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace AssetBundleFramework.Editor
{
    //xml 文件要序列化（？
    public class BuildSetting : ISupportInitialize
    {
        [DisplayName("项目名称")]
        [XmlAttribute("ProjectName")]
        public string projectName { get; set; }

        [DisplayName("后缀列表")]
        [XmlAttribute("SuffixList")]
        public List<string> suffixList { get; set; } = new List<string>();

        [DisplayName("打包文件的目录文件夹")]
        [XmlAttribute("BuildRoot")]
        public string buildRoot { get; set; }

        [DisplayName("打包选项")]
        [XmlElement("BuildItem")]
        public List<BuildItem> items { get; set; } = new List<BuildItem>();

        //每打一个小包都会有一些信息存在BuildItem里
        //字典
        [XmlIgnore]
        public Dictionary<string, BuildItem> itemDic = new Dictionary<string, BuildItem>();

        public void BeginInit()
        {

        }

        public void EndInit()
        {
            buildRoot = Path.GetFullPath(buildRoot).Replace("\\", "/");

            itemDic.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                BuildItem buildItem = items[i];

                //检查BundleType
                if (buildItem.bundleType == EBundleType.All || buildItem.bundleType == EBundleType.Directory)
                {
                    if (!Directory.Exists(buildItem.assetPath))
                    {
                        throw new System.Exception($"不存在资源路径:{buildItem.assetPath}");
                    }
                }

                //根据后缀处理
                string[] prefixes = buildItem.suffix.Split('|');
                for(int ii = 0; ii < prefixes.Length; ii++)
                {
                    string prefix = prefixes[ii].Trim();//从当前字符串删除所有前导空白字符和尾随空白字符
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        //加后缀
                        buildItem.suffixes.Add(prefix);
                    }
                }

                //有path的key 要抛异常
                if (itemDic.ContainsKey(buildItem.assetPath))
                {
                    throw new System.Exception($"重复的资源路径:{buildItem.assetPath}");
                }
                //加入字典
                itemDic.Add(buildItem.assetPath, buildItem);
            }
        }

        /// <summary>
        /// 6.获取所有在打包设置的文件列表
        /// 首先需要知道打包规则
        /// </summary>
        /// <returns></returns>
        public HashSet<string> Collect()
        {
            //最小进度，在打包规则里面设置好的
            float min = Builder.collectRuleFileProgress.x;
            //最大进度
            float max = Builder.collectRuleFileProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(Collect)}","搜集打包规则资源",min);

            //处理每个规则忽略的目录（规则？，比如路径A/B/C,需要忽略A/B
            //遍历一遍
            for(int i = 0; i < items.Count; i++)
            {
                BuildItem buildItem_i = items[i];
                if(buildItem_i.resourceType != EResourceType.Direct)
                {
                    //跳过
                    continue;
                }

                buildItem_i.ignorePaths.Clear();
                for(int j = 0; j< items.Count; j++)
                {
                    BuildItem buildItem_j = items[j];
                    //资源不相等 且 该资源也属于打包资源里的东西
                    if(i != j&& buildItem_j.resourceType == EResourceType.Direct)
                    {
                        //如果j的开头是i的话
                        if(buildItem_j.assetPath.StartsWith(buildItem_i.assetPath,StringComparison.InvariantCulture))
                        {
                            //i忽略j的意思是为了防止打AB的时候把j的内容打进i里？
                            buildItem_i.ignorePaths.Add(buildItem_j.assetPath);
                        }
                    }
                }
            }

            //存储被规则分析到的所有文件
            HashSet<string> files = new HashSet<string>();
            for(int i = 0; i < items.Count; i++)
            {
                BuildItem buildItem = items[i];

                EditorUtility.DisplayProgressBar($"{nameof(Collect)}", "搜集打包规则资源", min + (max - min) * ((float)i/items.Count -1));
            
                if(buildItem.resourceType != EResourceType.Direct)
                {
                    //忽略非Direct的
                    continue;
                }

                List<string> tempFiles = Builder.GetFiles(buildItem.assetPath, null, buildItem.suffixes.ToArray());
                for(int j = 0; j < tempFiles.Count; j++)
                {
                    string file = tempFiles[j];

                    //过滤被忽略的，在忽略列表里就不能用啦
                    if (IsIgnore(buildItem.ignorePaths, file))
                    {
                        continue;
                    }

                    files.Add(file);
                }

                //在里面标记下进度
                EditorUtility.DisplayProgressBar($"{nameof(Collect)}", "搜集打包设置资源", (float)(i + i) / items.Count);
            }
            return files;
        }


        /// <summary>
        /// 7.文件是否在忽略列表里
        /// </summary>
        /// <param name="ignoreList"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool IsIgnore(List<string> ignoreList,string file)
        {
            //遍历忽略列表
            for(int i = 0; i < ignoreList.Count; i++)
            {
                //获取忽略路径
                string ignorePath = ignoreList[i];
                //如果字符串为null或者空字符串""
                if (string.IsNullOrEmpty(ignorePath))
                {
                    continue;
                }

                //如果前缀有ignorePath 那么就应该是要被忽略的
                if (file.StartsWith(ignorePath, StringComparison.InvariantCulture))
                {
                    return true;
                }
            }
            return false;
        }

        public BuildItem GetBuildItem(string assetUrl)
        {
            BuildItem item = null;
            for(int i = 0; i < items.Count; i++)
            {
                BuildItem tempItem = items[i];
                //前面是否匹配
                if (assetUrl.StartsWith(tempItem.assetPath, StringComparison.InvariantCulture))
                {
                    //找到优先级最高的rule,路径越长说明优先级越高（为什么啊
                    if(item == null || item.assetPath.Length<tempItem.assetPath.Length)
                    {
                        item = tempItem;
                    }
                }
            }
            return item;
        }


        /// <summary>
        /// 8?获取BundleName
        /// </summary>
        /// <returns></returns>

        public string GetBundleName(string assetUrl, EResourceType resourceType)
        {
            BuildItem buildItem = GetBuildItem(assetUrl);
            if(buildItem == null)
            {
                return null;
            }

            string name = "";

            if (buildItem.resourceType == EResourceType.Dependency)
            {
                string extension = Path.GetExtension(assetUrl).ToLower();
                bool exist = false;

                for (int i = 0; i < buildItem.suffixes.Count; i++)
                {
                    //是在看其后缀是否在列表里
                    if (buildItem.suffixes[i] == extension)
                    {
                        exist = true;
                    }
                }

                if (!exist)
                {
                    return null;
                }
            }

            switch (buildItem.bundleType)
            {
                case EBundleType.All:
                    name = buildItem.assetPath;
                    if (buildItem.assetPath[buildItem.assetPath.Length - 1] == '/')
                    {
                        name = buildItem.assetPath.Substring(0, buildItem.assetPath.Length - 1);
                    }
                    name = $"{name}{Builder.BUNDLE_SUFFIX}".ToLowerInvariant();
                    break;
                case EBundleType.Directory:
                    name = $"{assetUrl.Substring(0, assetUrl.LastIndexOf('/'))}{Builder.BUNDLE_SUFFIX}".ToLowerInvariant();
                    break;
                case EBundleType.File:
                    name = $"{assetUrl}{Builder.BUNDLE_SUFFIX}".ToLowerInvariant();
                    break;
                default:
                    throw new Exception($"无法获取{assetUrl}的BundleName");
            }

            buildItem.Count += 1;

            return name;
        }
    }


}
