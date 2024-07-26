using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AssetBundleFramework.Core;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AssetBundleFramework.Editor
{
    public static class Builder
    {
        //��ߵ�100%����������
        public static readonly Vector2 collectRuleFileProgress = new Vector2(0, 0.2f);
        public static readonly Vector2 ms_GetDependencyProgress = new Vector2(0.2f, 0.4f);
        public static readonly Vector2 ms_CollectBundleInfoProgress = new Vector2(0.4f, 0.5f);
        public static readonly Vector2 ms_GenerateBuildInfoProgress = new Vector2(0.5f, 0.6f);
        public static readonly Vector2 ms_BuildBundleProgress = new Vector2(0.6f, 0.7f);
        public static readonly Vector2 ms_ClearBundleProgress = new Vector2(0.7f, 0.9f);
        public static readonly Vector2 ms_BuildManifestProgress = new Vector2(0.9f, 1f);


        private static readonly Profiler ms_BuildProfiler = new Profiler(nameof(Builder));
        private static readonly Profiler ms_LoadBuildSettingProfiler = ms_BuildProfiler.CreateChild(nameof(LoadSetting));
        private static readonly Profiler ms_SwitchPlatformProfiler = ms_BuildProfiler.CreateChild(nameof(SwitchPlatform));
        private static readonly Profiler ms_CollectProfiler = ms_BuildProfiler.CreateChild(nameof(Collect));
        /// <summary>
        /// �Ѽ����BuildSetting�ļ��Ĺ��̷���
        /// </summary>
        private static readonly Profiler ms_CollectBuildSettingFileProfiler = ms_CollectProfiler.CreateChild("CollectBuildSettingFile");
        /// <summary>
        /// �����
        /// </summary>
        private static readonly Profiler ms_CollectDependencyProfiler = ms_CollectProfiler.CreateChild(nameof(CollectDependency));
        /// <summary>
        /// ����Bundle��
        /// </summary>
        private static readonly Profiler ms_CollectBundleProfiler = ms_CollectProfiler.CreateChild(nameof(CollectBundle));

        private static readonly Profiler ms_GenerateManifestProfiler = ms_CollectProfiler.CreateChild(nameof(GenerateManifest));

        private static readonly Profiler ms_BuildBundleProfiler = ms_CollectProfiler.CreateChild(nameof(BuildBundle));
        private static readonly Profiler ms_ClearBundleProfiler = ms_CollectProfiler.CreateChild(nameof(ClearAssetBundle));
        private static readonly Profiler ms_BuildManifestBundleProfiler = ms_CollectProfiler.CreateChild(nameof(BuildManifest));



#if UNITY_IOS
    private const string PLATFORM ="IOS";
#elif UNITY_ANDROID
    private const string PLATFORM ="ANDROID";
#else
        private const string PLATFORM = "windows";
#endif

        //bundle��׺
        public const string BUNDLE_SUFFIX = ".ab";
        public const string BUNDLE_MANIFEST_SUFFIX = ".manifest";
        //bundle�����ļ�����
        public const string MANIFEST = "manifest";


        public static readonly ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2
        };

        //manifest ��Ҫ���޳�

        //Bundle�����Options
        public readonly static BuildAssetBundleOptions buildAssetBundleOptions =
            BuildAssetBundleOptions.ChunkBasedCompression |
            BuildAssetBundleOptions.DeterministicAssetBundle |
            BuildAssetBundleOptions.StrictMode |
            BuildAssetBundleOptions.DisableLoadAssetByFileName |
            BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;

        /// <summary>
        /// �������
        /// </summary>
        public static BuildSetting buildSetting { get; private set; }

        /// <summary>
        /// ���Ŀ¼
        /// </summary>
        public static string buildPath { get; set; }

        /// <summary>
        /// ��ʱĿ¼����ʱ���ɵ��ļ���ͳһ���ڸ�Ŀ¼
        /// </summary>
        public readonly static string TempPath = Path.GetFullPath(Path.Combine(Application.dataPath, "Temp")).Replace("\\", "/");
        //dataPath��ͨ��·��

        /// <summary>
        /// ��Դ����__�ı�
        /// </summary>
        public readonly static string ResourcePath_Text = $"{TempPath}/Resource.txt";

        /// <summary>
        /// ��Դ����__�������ļ�
        /// </summary>
        public readonly static string ResourcePath_Binary = $"{TempPath}/Resource.byte";

        /// <summary>
        /// Bundle����__�ı�
        /// </summary>
        public readonly static string BundlePath_Text = $"{TempPath}/Bundle.txt";

        /// <summary>
        /// Bundle����__�������ļ�
        /// </summary>
        public readonly static string BundlePath_Binary = $"{TempPath}/Bundle.byte";

        /// <summary>
        /// ��Դ��������__�ı�
        /// </summary>
        public readonly static string DependencyPath_Text = $"{TempPath}/Dependency.txt";

        /// <summary>
        /// ��Դ��������__�������ļ�
        /// </summary>
        public readonly static string DependencyPath_Binary = $"{TempPath}/Dependency.byte";

        /// <summary>
        /// �������
        /// </summary>
        public readonly static string BuildSettingPath = Path.GetFullPath("BuildSetting.xml").Replace("\\", "/");

        /// <summary>
        /// ��ʱĿ¼����ʱ�ļ���ab�����������ļ��У������ɺ���Ƴ�
        /// </summary>
        public readonly static string TempBuildPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../TempBuild")).Replace("\\", "/");


        #region Build Menu Item
        [MenuItem("Tool/ResBuild/Windows")]
        public static void BuildWindow()
        {
            Debug.Log("Execute Build Window");
            Build();
        }

        public static void SwitchPlatform()
        {
            string platform = PLATFORM;

            switch (platform)
            {
                case "windows":
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,
                        BuildTarget.StandaloneWindows64);
                    break;
                case "android":
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,
                        BuildTarget.Android);
                    break;
                case "ios":
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,
                        BuildTarget.iOS);
                    break;
            }
            //Ҫ����BuildSetting,����ʲô����һ��xml��
        }

        //��ȡBuildSetting
        private static BuildSetting LoadSetting(string settingPath)
        {
            buildSetting = XmlUtility.Read<BuildSetting>(settingPath);
            if (buildSetting == null)
            {
                throw new Exception($"Load buildSetting failed, SettingPath:{settingPath}");
            }
            //����ȡ����BuildSetting�Ƿ���תISupportInitialize
            (buildSetting as ISupportInitialize)?.EndInit();

            //�õ�����·��
            //Ȼ����·����ת��
            //Ŀ¼����Ҫ ֮�������bundle

            ///if()
            //·��
            buildPath = Path.GetFullPath(buildSetting.buildRoot).Replace("\\", "/");
            if (buildPath.Length > 0 && buildPath[buildPath.Length - 1] != '/')
            {
                buildPath += "/";
            }
            buildPath += $"{PLATFORM}/";

            return buildSetting;
        }

        private static void Build()
        {
            ms_BuildProfiler.Start();

            //�л�ƽ̨
            ms_SwitchPlatformProfiler.Start();
            SwitchPlatform();
            ms_SwitchPlatformProfiler.Stop();

            //����BuildSetting
            ms_LoadBuildSettingProfiler.Start();
            buildSetting = LoadSetting(BuildSettingPath);
            ms_LoadBuildSettingProfiler.Stop();

            //�Ѽ�Bundle��Ϣ
            ms_CollectProfiler.Start();
            Dictionary<string, List<string>> bundleDic = Collect();
            ms_CollectProfiler.Stop();


            //���AssetBundle
            ms_BuildBundleProfiler.Start();
            BuildBundle(bundleDic);
            ms_BuildBundleProfiler.Stop();

            //��������ļ�
            ms_ClearBundleProfiler.Start();
            ClearAssetBundle(buildPath, bundleDic);
            ms_ClearBundleProfiler.Stop();

            //�������ļ����Bundle
            ms_BuildManifestBundleProfiler.Start();
            BuildManifest();
            ms_BuildManifestBundleProfiler.Stop();

            EditorUtility.ClearProgressBar();

            ms_BuildProfiler.Stop();
            Debug.Log($"������{ms_BuildProfiler}");
        }

        /// <summary>
        /// �Ѽ����Bundle��Ϣ
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, List<string>> Collect()
        {
            //��ȡ�����ڴ�����õ��ļ��б�
            ms_CollectBuildSettingFileProfiler.Start();
            //����һ��hashSet,Ҳ����ͨ��buildSetting���� Ȼ��浽����
            HashSet<string> files = buildSetting.Collect();
            ms_CollectBuildSettingFileProfiler.Stop();

            //�Ѽ������ļ���������ϵ���о����鷳����
            ms_CollectDependencyProfiler.Start();
            Dictionary<string, List<string>> dependencyDic = CollectDependency(files);
            ms_CollectDependencyProfiler.Stop();

            //8.���ڱ��������Դ����Ϣ
            Dictionary<string, EResourceType> assetDic = new Dictionary<string, EResourceType>();

            //��������÷������� ֱ������ΪDirect
            foreach (string url in files)
            {
                assetDic.Add(url, EResourceType.Direct);
                Debug.Log(url + " Direct");

            }

            //��������Դ�򱻱��ΪDependency���Ѿ����ڵ�˵������Direct����Դ
            foreach (string url in dependencyDic.Keys)
            {
                //�Ѿ����˾Ͳ�����
                if (!assetDic.ContainsKey(url))
                {
                    assetDic.Add(url, EResourceType.Dependency);
                    Debug.Log(url + " Dependency");
                }
            }

            //���ֶα���Bundle��Ӧ����Դ����
            ms_CollectBundleProfiler.Start();
            Dictionary<string, List<string>> bundleDic = CollectBundle(buildSetting, assetDic, dependencyDic);
            ms_CollectBundleProfiler.Stop();

            //����Manifest�ļ�
            ms_GenerateManifestProfiler.Start();
            GenerateManifest(assetDic, bundleDic, dependencyDic);
            ms_GenerateManifestProfiler.Stop();

            return bundleDic;
        }

        /// <summary>
        /// �Ѽ�ָ���ļ����ϵ�����������Ϣ
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private static Dictionary<string, List<string>> CollectDependency(ICollection<string> files)
        {
            //��ǽ���
            float min = ms_GetDependencyProgress.x;
            float max = ms_GetDependencyProgress.y;

            Dictionary<string, List<string>> dependencyDic = new Dictionary<string, List<string>>();

            //����fileList�󣬲���Ҫ�ع񣿵ݹ�?��
            List<string> fileList = new List<string>(files);

            for (int i = 0; i < fileList.Count; i++)
            {
                string assetUrl = fileList[i];
                //��������ֵ��Ѿ����� �Ͳ��ü��˴���ǣ�
                if (dependencyDic.ContainsKey(assetUrl))
                {
                    continue;
                }

                //�����ļ������������,ֻ�Ǵ�ŵ�ģ�� ���Գ�����3 emm
                if (i % 10 == 0)
                {
                    float progress = min + (max - min) * ((float)i / (files.Count * 3));
                    EditorUtility.DisplayProgressBar($"{nameof(CollectDependency)}", "�Ѽ�������Ϣ", progress);
                }

                //��Ҫ��ȡ�����ĵط�
                string[] dependencies = AssetDatabase.GetDependencies(assetUrl, false);
                List<string> dependencyList = new List<string>(dependencies.Length);

                //���˵�������Ҫ���
                for (int ii = 0; ii < dependencies.Length; ii++)
                {
                    string tempAssetUrl = dependencies[ii];
                    string extension = Path.GetExtension(tempAssetUrl).ToLower();
                    //���Ϊ�գ�����Ǵ��룬�����dll�� ����Ҫ
                    if (string.IsNullOrEmpty(extension) || extension == ".cs" || extension == ".dll")
                    // || extension == ".shader" || extension == ".mat")
                    {
                        continue;
                    }
                    dependencyList.Add(tempAssetUrl);
                    //����ֹ�ظ�
                    if (!fileList.Contains(tempAssetUrl))
                    {
                        fileList.Add(tempAssetUrl);
                    }
                }

                //assetUrlΪkey,����������Դ��value
                dependencyDic.Add(assetUrl, dependencyList);
            }
            return dependencyDic;
        }

        private static Dictionary<string, List<string>> CollectBundle(BuildSetting buildSetting, Dictionary<string, EResourceType> assetDic, Dictionary<string, List<string>> dependencyDic)
        {
            //��ǽ���
            float min = ms_CollectBundleInfoProgress.x;
            float max = ms_CollectBundleInfoProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(CollectBundle)}", "�Ѽ�bundle��Ϣ", min);
            Dictionary<string, List<string>> bundleDic = new Dictionary<string, List<string>>();

            //���ڴ洢�ⲿ��Դ
            List<string> notInRuleList = new List<string>();

            int index = 0;
            foreach (KeyValuePair<string, EResourceType> pair in assetDic)
            {
                index++;
                string assetUrl = pair.Key;
                string bundleName = buildSetting.GetBundleName(assetUrl, pair.Value);

                //��������Դ��û��bundleName��,��ô���ͻᱻ��Ϊ�ⲿ��Դ
                if (bundleName == null)
                {
                    notInRuleList.Add(assetUrl);
                    continue;
                }

                List<string> list;
                //���ȡ����
                if (!bundleDic.TryGetValue(bundleName, out list))
                {
                    list = new List<string>();
                    bundleDic.Add(bundleName, list);
                }
                //��Ϊ���������Ի����ܳɹ��ӽ�ȥ�ġ�����?�����Է�������𲻻ᱨ�գ�
                //�ǵ� ���ᱨ�� ��Ϊout�˸�list
                list.Add(assetUrl);

                EditorUtility.DisplayProgressBar($"{nameof(CollectBundle)}", "�Ѽ�bundle��Ϣ", min + (max - min) * ((float)index / assetDic.Count));

            }

            //�����ⲿ�쳣
            if (notInRuleList.Count > 0)
            {
                string message = string.Empty;//�����������µı����������ǲ����䴢��ռ䣿""���Ƿ���һ������Ϊ�յĴ���ռ䣬�����е�����
                for (int i = 0; i < notInRuleList.Count; i++)
                {
                    message += "\n" + notInRuleList[i];
                }
                EditorUtility.ClearProgressBar();//�������ʱ�� �������Ͳ�Ӧ����ʾ��
                throw new Exception($"��Դ���ڴ�����򣬻��ߺ�׺��ƥ�䣡��{message}");
            }

            //����
            foreach (List<string> list in bundleDic.Values)
            {
                list.Sort();
            }

            return bundleDic;
        }

        //9.������Դ�����ļ�  Manifest
        //��������֮ǰ���ɵĶ���
        private static void GenerateManifest(Dictionary<string, EResourceType> assetDic,
            Dictionary<string, List<string>> bundleDic, Dictionary<string, List<string>> dependencyDic)
        {
            //������
            float min = ms_GenerateBuildInfoProgress.x;
            float max = ms_GenerateBuildInfoProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "���ɴ����Ϣ", min);

            //������ʱ����ļ���Ŀ¼(��������ڵĻ�)
            if (!Directory.Exists(TempPath))
            {
                Directory.CreateDirectory(TempPath);
            }

            //��Դӳ��id
            Dictionary<string, ushort> assetIdDic = new Dictionary<string, ushort>();

            #region ������Դ������Ϣ
            {
                //ɾ����Դ�����ı��ļ�
                if (File.Exists(ResourcePath_Text))
                {
                    File.Delete(ResourcePath_Text);
                }

                //ɾ����Դ�����������ļ�
                if (File.Exists(ResourcePath_Binary))
                {
                    File.Delete(ResourcePath_Binary);
                }

                //д����Դ�б�
                StringBuilder resourceSb = new StringBuilder();
                MemoryStream resourceMs = new MemoryStream();
                BinaryWriter resourceBw = new BinaryWriter(resourceMs);
                if (assetDic.Count > ushort.MaxValue)
                {
                    EditorUtility.ClearProgressBar();
                    throw new Exception($"��Դ��������{ushort.MaxValue}");
                }

                //д�����
                resourceBw.Write((ushort)assetDic.Count);
                List<string> keys = new List<string>(assetDic.Keys);
                keys.Sort();

                for (ushort i = 0; i < keys.Count; i++)
                {
                    string assetUrl = keys[i];
                    assetIdDic.Add(assetUrl, i);
                    resourceSb.AppendLine($"{i}\t{assetUrl}");//\t����˼��ˮƽ�Ʊ��
                    resourceBw.Write(assetUrl);
                }
                resourceMs.Flush();//����ʲô
                byte[] buffer = resourceMs.GetBuffer();
                resourceBw.Close();
                //д����Դ�����ı��ļ�
                File.WriteAllText(ResourcePath_Text, resourceSb.ToString(), Encoding.UTF8);
                File.WriteAllBytes(ResourcePath_Binary, buffer);

            }
            #endregion

            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "���ɴ����Ϣ", min + (max - min) * 0.3f);

            #region ����bundle������Ϣ
            {
                //ɾ��Bundle�����ı��ļ�
                if (File.Exists(BundlePath_Text))
                {
                    File.Delete(BundlePath_Text);
                }

                //ɾ����Դ�����������ļ�
                if (File.Exists(BundlePath_Binary))
                {
                    File.Delete(BundlePath_Binary);
                }

                //д��Bundle��Ϣ�б�
                StringBuilder bundleSb = new StringBuilder();
                MemoryStream bundleMs = new MemoryStream();
                BinaryWriter bundleBw = new BinaryWriter(bundleMs);

                //д��Bundle����
                bundleBw.Write((ushort)bundleDic.Count);

                foreach (var kv in bundleDic)
                {
                    string bundleName = kv.Key;
                    List<string> assets = kv.Value;

                    //д��bundle
                    bundleSb.AppendLine(bundleName);
                    bundleBw.Write(bundleName);

                    //д����Դ����
                    bundleBw.Write((ushort)assets.Count);

                    for (int i = 0; i < assets.Count; i++)
                    {
                        string assetUrl = assets[i];
                        ushort assetId = assetIdDic[assetUrl];
                        bundleSb.AppendLine($"\t{assetUrl}");
                        bundleBw.Write(assetId);
                    }

                }

                bundleMs.Flush();
                byte[] buffer = bundleMs.GetBuffer();
                bundleBw.Close();

                //д��Bundle�����ı��ļ�
                File.WriteAllText(BundlePath_Text, bundleSb.ToString(), Encoding.UTF8);
                File.WriteAllBytes(BundlePath_Binary, buffer);
            }
            #endregion

            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "���ɴ����Ϣ", min + (max - min) * 0.8f);

            #region ������Դ����������Ϣ
            {
                //ɾ�����������ı��ļ�
                if (File.Exists(DependencyPath_Text))
                {
                    File.Delete(DependencyPath_Text);
                }

                //ɾ�����������������ļ�
                if (File.Exists(DependencyPath_Binary))
                {
                    File.Delete(DependencyPath_Binary);
                }

                //д����Դ������Ϣ�б�
                StringBuilder dependencySb = new StringBuilder();
                MemoryStream dependencyMs = new MemoryStream();
                BinaryWriter dependencyBw = new BinaryWriter(dependencyMs);

                //���ڱ�����Դ������
                List<List<ushort>> dependencyList = new List<List<ushort>>();

                foreach (var kv in dependencyDic)
                {
                    List<string> dependencyAssets = kv.Value;

                    if (dependencyAssets.Count == 0)
                    {
                        //û������ ����Ҫ
                        continue;
                    }

                    string assetUrl = kv.Key;

                    //��ʱ��List
                    List<ushort> ids = new List<ushort>();
                    ids.Add(assetIdDic[assetUrl]);

                    string content = assetUrl;
                    for (int i = 0; i < dependencyAssets.Count; i++)
                    {
                        string dependencyAssetUrl = dependencyAssets[i];
                        content += $"\t{dependencyAssetUrl}";
                        ids.Add(assetIdDic[dependencyAssetUrl]);
                    }

                    dependencySb.AppendLine(content);

                    if (ids.Count > byte.MaxValue)
                    {
                        EditorUtility.ClearProgressBar();
                        throw new Exception($"��Դ{assetUrl}����������һ���ֽڵ�����:{byte.MaxValue}");
                    }

                    dependencyList.Add(ids);
                }

                //д������������
                dependencyBw.Write((ushort)dependencyList.Count);
                for (int i = 0; i < dependencyList.Count; i++)
                {
                    //д����Դ��
                    List<ushort> ids = dependencyList[i];
                    dependencyBw.Write((ushort)ids.Count);
                    for (int ii = 0; ii < ids.Count; ii++)
                    {
                        dependencyBw.Write(ids[ii]);
                    }
                }
                dependencyMs.Flush();
                byte[] buffer = dependencyMs.GetBuffer();
                dependencyBw.Close();

                //д�����������ı��ļ�
                File.WriteAllText(DependencyPath_Text, dependencySb.ToString(), Encoding.UTF8);
                File.WriteAllBytes(DependencyPath_Binary, buffer);
            }
            #endregion

            //AB��ˢ��
            AssetDatabase.Refresh();

            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "���ɴ����Ϣ", max);

        }

        /// <summary>
        /// ��ն����AssetBundle
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bundleDic"></param>
        public static void ClearAssetBundle(string path, Dictionary<string, List<string>> bundleDic)
        {
            //������
            float min = ms_GenerateBuildInfoProgress.x;
            float max = ms_GenerateBuildInfoProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(ClearAssetBundle)}", "��������AssetBundle�ļ�", min);

            //��ȡpath�µ�Ŀ¼ Ȼ��hashSet���ò��ظ��� Ȼ�����Bundle��Key���𲽰Ѷ������յ�

            //���Ȼ�ȡbuildPath���ļ��б� Ȼ����ݸ��ļ��б�ȥ��� ����Bundle��Ķ���������BuildPath���ǱȽϸɾ���

            List<string> fileList = GetFiles(path, null, null);//ǰ��׺��������
            HashSet<string> fileSet = new HashSet<string>(fileList); //HashSet

            //ȡbundleDic
            foreach (string bundle in bundleDic.Keys)
            {
                fileSet.Remove($"{path}{bundle}");
                fileSet.Remove($"{path}{bundle}{BUNDLE_MANIFEST_SUFFIX}");//�Ƴ�manifest�ļ���
            }

            fileSet.Remove($"{path}{PLATFORM}");//��Ӧƽ̨�µ�  ����û����
            fileSet.Remove($"{path}{PLATFORM}{BUNDLE_MANIFEST_SUFFIX}");//���ǣ�

            Parallel.ForEach(fileSet, parallelOptions, File.Delete);

            EditorUtility.DisplayProgressBar($"{nameof(ClearAssetBundle)}", "��������AssetBundle�ļ�", max);
        }


        /// <summary>
        /// ���AssetBundle
        /// </summary>
        /// <returns></returns>
        public static AssetBundleManifest BuildBundle(Dictionary<string, List<string>> bundleDic)
        {
            //������
            float min = ms_BuildBundleProgress.x;
            float max = ms_BuildBundleProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(BuildBundle)}", "���AssetBundle", min);

            //�����λ��
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(buildPath, GetBuilds(bundleDic),
                buildAssetBundleOptions, EditorUserBuildSettings.activeBuildTarget);

            EditorUtility.DisplayProgressBar($"{nameof(BuildBundle)}", "���AssetBundle", max);

            return manifest;
        }

        private static void BuildManifest()
        {
            //������
            float min = ms_BuildManifestProgress.x;
            float max = ms_BuildManifestProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(BuildManifest)}", "��Manifest�����AssetBundle", min);

            if (!Directory.Exists(TempBuildPath))
            {
                Directory.CreateDirectory(TempBuildPath);
            }

            //��Ҫ��ȡ�ϲ�Ŀ¼��������

            string prefix = Application.dataPath.Replace("/Assets", "/").Replace("\\", "/");

            AssetBundleBuild manifest = new AssetBundleBuild();
            manifest.assetBundleName = $"{MANIFEST}{BUNDLE_SUFFIX}";
            manifest.assetNames = new string[3]
            {
            ResourcePath_Binary.Replace(prefix,""),
            BundlePath_Binary.Replace(prefix,""),
            DependencyPath_Binary.Replace(prefix,""),
            };

            EditorUtility.DisplayProgressBar($"{nameof(BuildManifest)}", "��Manifest�����AssetBundle", min + (max - min) * 0.5f);

            AssetBundleManifest assetBundleManifest = BuildPipeline.BuildAssetBundles(TempBuildPath,
                new AssetBundleBuild[] { manifest }, buildAssetBundleOptions, EditorUserBuildSettings.activeBuildTarget);
            //��Щ��������֪����ʲôhh

            //���ļ�copy��buildĿ¼
            if (assetBundleManifest)
            {
                string manifestFile = $"{TempBuildPath}/{MANIFEST}{BUNDLE_SUFFIX}";
                string target = $"{buildPath}/{MANIFEST}/{BUNDLE_SUFFIX}";
                if (File.Exists(manifestFile))
                {
                    File.Copy(manifestFile, target);
                }
            }

            //ɾ����ʱĿ¼
            if (!Directory.Exists(TempBuildPath))
            {
                Directory.Delete(TempBuildPath, true);
            }

            EditorUtility.DisplayProgressBar($"{nameof(BuildManifest)}", "��Manifest�����AssetBundle", max);

        }

        /// <summary>
        /// ��ȡ������Ҫ�����AssetBundleBuild
        /// </summary>
        /// <param name="bundleTable"></param>
        /// <returns></returns>
        public static AssetBundleBuild[] GetBuilds(Dictionary<string, List<string>> bundleTable)
        {
            int index = 0;
            AssetBundleBuild[] assetBundleBuilds = new AssetBundleBuild[bundleTable.Count];
            foreach (KeyValuePair<string, List<string>> pair in bundleTable)
            {
                assetBundleBuilds[index++] = new AssetBundleBuild()
                {
                    //��������
                    assetBundleName = pair.Key,
                    assetNames = pair.Value.ToArray()
                };
            }
            return assetBundleBuilds;
        }


        /// <summary>
        /// 7.��ȡָ��·�����ļ�
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prefix"></param>
        /// <param name="suffies"></param>
        /// <returns></returns>
        public static List<string> GetFiles(string path, string prefix, params string[] suffixes)
        {
            //Directory.GetFiles�������ǻ�ȡpathĿ¼�е������ļ�
            string[] files = Directory.GetFiles(path, $"*.*", SearchOption.AllDirectories);
            //�����ļ�����
            List<string> result = new List<string>(files.Length);

            for (int i = 0; i < files.Length; i++)
            {
                //�л���ʽ
                string file = files[i].Replace('\\', '/');

                //�̶������Ե��ʱȽϹ��򣿣�
                //ǰ׺������Ƶ˵���Ǻ�׺��Ӧ�þ���ǰ׺
                if (prefix != null && file.StartsWith(prefix, StringComparison.InvariantCulture))
                {
                    continue;
                }

                if (suffixes != null && suffixes.Length > 0)
                {
                    //��׺������?
                    bool exist = false;

                    for (int ii = 0; ii < suffixes.Length; ii++)
                    {
                        string suffix = suffixes[ii];
                        if (file.EndsWith(suffix, StringComparison.InvariantCulture))
                        {
                            exist = true;
                            break;
                        }
                    }

                    if (!exist)
                    {
                        continue;
                    }
                }

                result.Add(file);
            }

            return result;
        }
        #endregion
    }

}


