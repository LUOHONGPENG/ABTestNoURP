using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace AssetBundleFramework.Editor
{
    //xml �ļ�Ҫ���л�����
    public class BuildSetting : ISupportInitialize
    {
        [DisplayName("��Ŀ����")]
        [XmlAttribute("ProjectName")]
        public string projectName { get; set; }

        [DisplayName("��׺�б�")]
        [XmlAttribute("SuffixList")]
        public List<string> suffixList { get; set; } = new List<string>();

        [DisplayName("����ļ���Ŀ¼�ļ���")]
        [XmlAttribute("BuildRoot")]
        public string buildRoot { get; set; }

        [DisplayName("���ѡ��")]
        [XmlElement("BuildItem")]
        public List<BuildItem> items { get; set; } = new List<BuildItem>();

        //ÿ��һ��С��������һЩ��Ϣ����BuildItem��
        //�ֵ�
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

                //���BundleType
                if (buildItem.bundleType == EBundleType.All || buildItem.bundleType == EBundleType.Directory)
                {
                    if (!Directory.Exists(buildItem.assetPath))
                    {
                        throw new System.Exception($"��������Դ·��:{buildItem.assetPath}");
                    }
                }

                //���ݺ�׺����
                string[] prefixes = buildItem.suffix.Split('|');
                for(int ii = 0; ii < prefixes.Length; ii++)
                {
                    string prefix = prefixes[ii].Trim();//�ӵ�ǰ�ַ���ɾ������ǰ���հ��ַ���β��հ��ַ�
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        //�Ӻ�׺
                        buildItem.suffixes.Add(prefix);
                    }
                }

                //��path��key Ҫ���쳣
                if (itemDic.ContainsKey(buildItem.assetPath))
                {
                    throw new System.Exception($"�ظ�����Դ·��:{buildItem.assetPath}");
                }
                //�����ֵ�
                itemDic.Add(buildItem.assetPath, buildItem);
            }
        }

        /// <summary>
        /// 6.��ȡ�����ڴ�����õ��ļ��б�
        /// ������Ҫ֪���������
        /// </summary>
        /// <returns></returns>
        public HashSet<string> Collect()
        {
            //��С���ȣ��ڴ�������������úõ�
            float min = Builder.collectRuleFileProgress.x;
            //������
            float max = Builder.collectRuleFileProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(Collect)}","�Ѽ����������Դ",min);

            //����ÿ��������Ե�Ŀ¼�����򣿣�����·��A/B/C,��Ҫ����A/B
            //����һ��
            for(int i = 0; i < items.Count; i++)
            {
                BuildItem buildItem_i = items[i];
                if(buildItem_i.resourceType != EResourceType.Direct)
                {
                    //����
                    continue;
                }

                buildItem_i.ignorePaths.Clear();
                for(int j = 0; j< items.Count; j++)
                {
                    BuildItem buildItem_j = items[j];
                    //��Դ����� �� ����ԴҲ���ڴ����Դ��Ķ���
                    if(i != j&& buildItem_j.resourceType == EResourceType.Direct)
                    {
                        //���j�Ŀ�ͷ��i�Ļ�
                        if(buildItem_j.assetPath.StartsWith(buildItem_i.assetPath,StringComparison.InvariantCulture))
                        {
                            //i����j����˼��Ϊ�˷�ֹ��AB��ʱ���j�����ݴ��i�
                            buildItem_i.ignorePaths.Add(buildItem_j.assetPath);
                        }
                    }
                }
            }

            //�洢������������������ļ�
            HashSet<string> files = new HashSet<string>();
            for(int i = 0; i < items.Count; i++)
            {
                BuildItem buildItem = items[i];

                EditorUtility.DisplayProgressBar($"{nameof(Collect)}", "�Ѽ����������Դ", min + (max - min) * ((float)i/items.Count -1));
            
                if(buildItem.resourceType != EResourceType.Direct)
                {
                    //���Է�Direct��
                    continue;
                }

                List<string> tempFiles = Builder.GetFiles(buildItem.assetPath, null, buildItem.suffixes.ToArray());
                for(int j = 0; j < tempFiles.Count; j++)
                {
                    string file = tempFiles[j];

                    //���˱����Եģ��ں����б���Ͳ�������
                    if (IsIgnore(buildItem.ignorePaths, file))
                    {
                        continue;
                    }

                    files.Add(file);
                }

                //���������½���
                EditorUtility.DisplayProgressBar($"{nameof(Collect)}", "�Ѽ����������Դ", (float)(i + i) / items.Count);
            }
            return files;
        }


        /// <summary>
        /// 7.�ļ��Ƿ��ں����б���
        /// </summary>
        /// <param name="ignoreList"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool IsIgnore(List<string> ignoreList,string file)
        {
            //���������б�
            for(int i = 0; i < ignoreList.Count; i++)
            {
                //��ȡ����·��
                string ignorePath = ignoreList[i];
                //����ַ���Ϊnull���߿��ַ���""
                if (string.IsNullOrEmpty(ignorePath))
                {
                    continue;
                }

                //���ǰ׺��ignorePath ��ô��Ӧ����Ҫ�����Ե�
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
                //ǰ���Ƿ�ƥ��
                if (assetUrl.StartsWith(tempItem.assetPath, StringComparison.InvariantCulture))
                {
                    //�ҵ����ȼ���ߵ�rule,·��Խ��˵�����ȼ�Խ�ߣ�Ϊʲô��
                    if(item == null || item.assetPath.Length<tempItem.assetPath.Length)
                    {
                        item = tempItem;
                    }
                }
            }
            return item;
        }


        /// <summary>
        /// 8?��ȡBundleName
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
                    //���ڿ����׺�Ƿ����б���
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
                    throw new Exception($"�޷���ȡ{assetUrl}��BundleName");
            }

            buildItem.Count += 1;

            return name;
        }
    }


}
