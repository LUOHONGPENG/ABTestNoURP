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
        //这边到100%就是完事了
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
        /// 搜集打包BuildSetting文件的过程分析
        /// </summary>
        private static readonly Profiler ms_CollectBuildSettingFileProfiler = ms_CollectProfiler.CreateChild("CollectBuildSettingFile");
        /// <summary>
        /// 依赖项？
        /// </summary>
        private static readonly Profiler ms_CollectDependencyProfiler = ms_CollectProfiler.CreateChild(nameof(CollectDependency));
        /// <summary>
        /// 生成Bundle的
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

        //bundle后缀
        public const string BUNDLE_SUFFIX = ".ab";
        public const string BUNDLE_MANIFEST_SUFFIX = ".manifest";
        //bundle描述文件名称
        public const string MANIFEST = "manifest";


        public static readonly ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2
        };

        //manifest 需要被剔除

        //Bundle打包的Options
        public readonly static BuildAssetBundleOptions buildAssetBundleOptions =
            BuildAssetBundleOptions.ChunkBasedCompression |
            BuildAssetBundleOptions.DeterministicAssetBundle |
            BuildAssetBundleOptions.StrictMode |
            BuildAssetBundleOptions.DisableLoadAssetByFileName |
            BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;

        /// <summary>
        /// 打包设置
        /// </summary>
        public static BuildSetting buildSetting { get; private set; }

        /// <summary>
        /// 打包目录
        /// </summary>
        public static string buildPath { get; set; }

        /// <summary>
        /// 临时目录，临时生成的文件都统一放在该目录
        /// </summary>
        public readonly static string TempPath = Path.GetFullPath(Path.Combine(Application.dataPath, "Temp")).Replace("\\", "/");
        //dataPath是通用路径

        /// <summary>
        /// 资源描述__文本
        /// </summary>
        public readonly static string ResourcePath_Text = $"{TempPath}/Resource.txt";

        /// <summary>
        /// 资源描述__二进制文件
        /// </summary>
        public readonly static string ResourcePath_Binary = $"{TempPath}/Resource.byte";

        /// <summary>
        /// Bundle描述__文本
        /// </summary>
        public readonly static string BundlePath_Text = $"{TempPath}/Bundle.txt";

        /// <summary>
        /// Bundle描述__二进制文件
        /// </summary>
        public readonly static string BundlePath_Binary = $"{TempPath}/Bundle.byte";

        /// <summary>
        /// 资源依赖描述__文本
        /// </summary>
        public readonly static string DependencyPath_Text = $"{TempPath}/Dependency.txt";

        /// <summary>
        /// 资源依赖描述__二进制文件
        /// </summary>
        public readonly static string DependencyPath_Binary = $"{TempPath}/Dependency.byte";

        /// <summary>
        /// 打包配置
        /// </summary>
        public readonly static string BuildSettingPath = Path.GetFullPath("BuildSetting.xml").Replace("\\", "/");

        /// <summary>
        /// 临时目录，临时文件的ab包都放在这文件夹，打包完成后会移除
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
            //要返回BuildSetting,那是什么，是一个xml？
        }

        //读取BuildSetting
        private static BuildSetting LoadSetting(string settingPath)
        {
            buildSetting = XmlUtility.Read<BuildSetting>(settingPath);
            if (buildSetting == null)
            {
                throw new Exception($"Load buildSetting failed, SettingPath:{settingPath}");
            }
            //检查读取到的BuildSetting是否能转ISupportInitialize
            (buildSetting as ISupportInitialize)?.EndInit();

            //拿到整个路径
            //然后做路径的转换
            //目录很重要 之后可能清bundle

            ///if()
            //路径
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

            //切换平台
            ms_SwitchPlatformProfiler.Start();
            SwitchPlatform();
            ms_SwitchPlatformProfiler.Stop();

            //加载BuildSetting
            ms_LoadBuildSettingProfiler.Start();
            buildSetting = LoadSetting(BuildSettingPath);
            ms_LoadBuildSettingProfiler.Stop();

            //搜集Bundle信息
            ms_CollectProfiler.Start();
            Dictionary<string, List<string>> bundleDic = Collect();
            ms_CollectProfiler.Stop();


            //打包AssetBundle
            ms_BuildBundleProfiler.Start();
            BuildBundle(bundleDic);
            ms_BuildBundleProfiler.Stop();

            //清理多余文件
            ms_ClearBundleProfiler.Start();
            ClearAssetBundle(buildPath, bundleDic);
            ms_ClearBundleProfiler.Stop();

            //把描述文件打包Bundle
            ms_BuildManifestBundleProfiler.Start();
            BuildManifest();
            ms_BuildManifestBundleProfiler.Stop();

            EditorUtility.ClearProgressBar();

            ms_BuildProfiler.Stop();
            Debug.Log($"打包完成{ms_BuildProfiler}");
        }

        /// <summary>
        /// 搜集打包Bundle信息
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, List<string>> Collect()
        {
            //获取所有在打包设置的文件列表
            ms_CollectBuildSettingFileProfiler.Start();
            //返回一个hashSet,也就是通过buildSetting分析 然后存到这里
            HashSet<string> files = buildSetting.Collect();
            ms_CollectBuildSettingFileProfiler.Stop();

            //搜集所有文件的依赖关系（感觉好麻烦啊）
            ms_CollectDependencyProfiler.Start();
            Dictionary<string, List<string>> dependencyDic = CollectDependency(files);
            ms_CollectDependencyProfiler.Stop();

            //8.用于标记所有资源的信息
            Dictionary<string, EResourceType> assetDic = new Dictionary<string, EResourceType>();

            //被打包配置分析到的 直接设置为Direct
            foreach (string url in files)
            {
                assetDic.Add(url, EResourceType.Direct);
                Debug.Log(url + " Direct");

            }

            //依赖的资源则被标记为Dependency，已经存在的说明就是Direct的资源
            foreach (string url in dependencyDic.Keys)
            {
                //已经有了就不加了
                if (!assetDic.ContainsKey(url))
                {
                    assetDic.Add(url, EResourceType.Dependency);
                    Debug.Log(url + " Dependency");
                }
            }

            //该字段保存Bundle对应的资源集合
            ms_CollectBundleProfiler.Start();
            Dictionary<string, List<string>> bundleDic = CollectBundle(buildSetting, assetDic, dependencyDic);
            ms_CollectBundleProfiler.Stop();

            //生成Manifest文件
            ms_GenerateManifestProfiler.Start();
            GenerateManifest(assetDic, bundleDic, dependencyDic);
            ms_GenerateManifestProfiler.Stop();

            return bundleDic;
        }

        /// <summary>
        /// 搜集指定文件集合的所有依赖信息
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private static Dictionary<string, List<string>> CollectDependency(ICollection<string> files)
        {
            //标记进度
            float min = ms_GetDependencyProgress.x;
            float max = ms_GetDependencyProgress.y;

            Dictionary<string, List<string>> dependencyDic = new Dictionary<string, List<string>>();

            //声明fileList后，不需要地柜？递归?了
            List<string> fileList = new List<string>(files);

            for (int i = 0; i < fileList.Count; i++)
            {
                string assetUrl = fileList[i];
                //如果依赖字典已经有了 就不用加了大概是？
                if (dependencyDic.ContainsKey(assetUrl))
                {
                    continue;
                }

                //根据文件数量计算进度,只是大概的模拟 所以乘以了3 emm
                if (i % 10 == 0)
                {
                    float progress = min + (max - min) * ((float)i / (files.Count * 3));
                    EditorUtility.DisplayProgressBar($"{nameof(CollectDependency)}", "搜集依赖信息", progress);
                }

                //主要获取依赖的地方
                string[] dependencies = AssetDatabase.GetDependencies(assetUrl, false);
                List<string> dependencyList = new List<string>(dependencies.Length);

                //过滤掉不符合要求的
                for (int ii = 0; ii < dependencies.Length; ii++)
                {
                    string tempAssetUrl = dependencies[ii];
                    string extension = Path.GetExtension(tempAssetUrl).ToLower();
                    //如果为空，如果是代码，如果是dll类 都不要
                    if (string.IsNullOrEmpty(extension) || extension == ".cs" || extension == ".dll")
                    // || extension == ".shader" || extension == ".mat")
                    {
                        continue;
                    }
                    dependencyList.Add(tempAssetUrl);
                    //检查防止重复
                    if (!fileList.Contains(tempAssetUrl))
                    {
                        fileList.Add(tempAssetUrl);
                    }
                }

                //assetUrl为key,所依赖的资源是value
                dependencyDic.Add(assetUrl, dependencyList);
            }
            return dependencyDic;
        }

        private static Dictionary<string, List<string>> CollectBundle(BuildSetting buildSetting, Dictionary<string, EResourceType> assetDic, Dictionary<string, List<string>> dependencyDic)
        {
            //标记进度
            float min = ms_CollectBundleInfoProgress.x;
            float max = ms_CollectBundleInfoProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(CollectBundle)}", "搜集bundle信息", min);
            Dictionary<string, List<string>> bundleDic = new Dictionary<string, List<string>>();

            //用于存储外部资源
            List<string> notInRuleList = new List<string>();

            int index = 0;
            foreach (KeyValuePair<string, EResourceType> pair in assetDic)
            {
                index++;
                string assetUrl = pair.Key;
                string bundleName = buildSetting.GetBundleName(assetUrl, pair.Value);

                //可能有资源是没有bundleName的,那么它就会被归为外部资源
                if (bundleName == null)
                {
                    notInRuleList.Add(assetUrl);
                    continue;
                }

                List<string> list;
                //如果取不到
                if (!bundleDic.TryGetValue(bundleName, out list))
                {
                    list = new List<string>();
                    bundleDic.Add(bundleName, list);
                }
                //因为是引用所以还是能成功加进去的···?但可以放这里的吗不会报空？
                //是的 不会报空 因为out了个list
                list.Add(assetUrl);

                EditorUtility.DisplayProgressBar($"{nameof(CollectBundle)}", "搜集bundle信息", min + (max - min) * ((float)index / assetDic.Count));

            }

            //处理外部异常
            if (notInRuleList.Count > 0)
            {
                string message = string.Empty;//不会再生成新的变量？好像是不分配储存空间？""则是分配一个长度为空的储存空间，还是有点消耗
                for (int i = 0; i < notInRuleList.Count; i++)
                {
                    message += "\n" + notInRuleList[i];
                }
                EditorUtility.ClearProgressBar();//有问题的时候 进度条就不应该显示了
                throw new Exception($"资源不在打包规则，或者后缀不匹配！！{message}");
            }

            //排序
            foreach (List<string> list in bundleDic.Values)
            {
                list.Sort();
            }

            return bundleDic;
        }

        //9.生成资源描述文件  Manifest
        //参数都是之前生成的东西
        private static void GenerateManifest(Dictionary<string, EResourceType> assetDic,
            Dictionary<string, List<string>> bundleDic, Dictionary<string, List<string>> dependencyDic)
        {
            //进度条
            float min = ms_GenerateBuildInfoProgress.x;
            float max = ms_GenerateBuildInfoProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "生成打包信息", min);

            //生成临时存放文件的目录(如果不存在的话)
            if (!Directory.Exists(TempPath))
            {
                Directory.CreateDirectory(TempPath);
            }

            //资源映射id
            Dictionary<string, ushort> assetIdDic = new Dictionary<string, ushort>();

            #region 生成资源描述信息
            {
                //删除资源描述文本文件
                if (File.Exists(ResourcePath_Text))
                {
                    File.Delete(ResourcePath_Text);
                }

                //删除资源描述二进制文件
                if (File.Exists(ResourcePath_Binary))
                {
                    File.Delete(ResourcePath_Binary);
                }

                //写入资源列表
                StringBuilder resourceSb = new StringBuilder();
                MemoryStream resourceMs = new MemoryStream();
                BinaryWriter resourceBw = new BinaryWriter(resourceMs);
                if (assetDic.Count > ushort.MaxValue)
                {
                    EditorUtility.ClearProgressBar();
                    throw new Exception($"资源个数超出{ushort.MaxValue}");
                }

                //写入个数
                resourceBw.Write((ushort)assetDic.Count);
                List<string> keys = new List<string>(assetDic.Keys);
                keys.Sort();

                for (ushort i = 0; i < keys.Count; i++)
                {
                    string assetUrl = keys[i];
                    assetIdDic.Add(assetUrl, i);
                    resourceSb.AppendLine($"{i}\t{assetUrl}");//\t的意思是水平制表符
                    resourceBw.Write(assetUrl);
                }
                resourceMs.Flush();//这是什么
                byte[] buffer = resourceMs.GetBuffer();
                resourceBw.Close();
                //写入资源描述文本文件
                File.WriteAllText(ResourcePath_Text, resourceSb.ToString(), Encoding.UTF8);
                File.WriteAllBytes(ResourcePath_Binary, buffer);

            }
            #endregion

            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "生成打包信息", min + (max - min) * 0.3f);

            #region 生成bundle描述信息
            {
                //删除Bundle描述文本文件
                if (File.Exists(BundlePath_Text))
                {
                    File.Delete(BundlePath_Text);
                }

                //删除资源描述二进制文件
                if (File.Exists(BundlePath_Binary))
                {
                    File.Delete(BundlePath_Binary);
                }

                //写入Bundle信息列表
                StringBuilder bundleSb = new StringBuilder();
                MemoryStream bundleMs = new MemoryStream();
                BinaryWriter bundleBw = new BinaryWriter(bundleMs);

                //写入Bundle个数
                bundleBw.Write((ushort)bundleDic.Count);

                foreach (var kv in bundleDic)
                {
                    string bundleName = kv.Key;
                    List<string> assets = kv.Value;

                    //写入bundle
                    bundleSb.AppendLine(bundleName);
                    bundleBw.Write(bundleName);

                    //写入资源个数
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

                //写入Bundle描述文本文件
                File.WriteAllText(BundlePath_Text, bundleSb.ToString(), Encoding.UTF8);
                File.WriteAllBytes(BundlePath_Binary, buffer);
            }
            #endregion

            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "生成打包信息", min + (max - min) * 0.8f);

            #region 生成资源依赖描述信息
            {
                //删除依赖描述文本文件
                if (File.Exists(DependencyPath_Text))
                {
                    File.Delete(DependencyPath_Text);
                }

                //删除依赖描述二进制文件
                if (File.Exists(DependencyPath_Binary))
                {
                    File.Delete(DependencyPath_Binary);
                }

                //写入资源依赖信息列表
                StringBuilder dependencySb = new StringBuilder();
                MemoryStream dependencyMs = new MemoryStream();
                BinaryWriter dependencyBw = new BinaryWriter(dependencyMs);

                //用于保存资源依赖链
                List<List<ushort>> dependencyList = new List<List<ushort>>();

                foreach (var kv in dependencyDic)
                {
                    List<string> dependencyAssets = kv.Value;

                    if (dependencyAssets.Count == 0)
                    {
                        //没有依赖 不需要
                        continue;
                    }

                    string assetUrl = kv.Key;

                    //临时的List
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
                        throw new Exception($"资源{assetUrl}的依赖超出一个字节的上限:{byte.MaxValue}");
                    }

                    dependencyList.Add(ids);
                }

                //写入依赖链个数
                dependencyBw.Write((ushort)dependencyList.Count);
                for (int i = 0; i < dependencyList.Count; i++)
                {
                    //写入资源数
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

                //写入依赖描述文本文件
                File.WriteAllText(DependencyPath_Text, dependencySb.ToString(), Encoding.UTF8);
                File.WriteAllBytes(DependencyPath_Binary, buffer);
            }
            #endregion

            //AB包刷新
            AssetDatabase.Refresh();

            EditorUtility.DisplayProgressBar($"{nameof(GenerateManifest)}", "生成打包信息", max);

        }

        /// <summary>
        /// 清空多余的AssetBundle
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bundleDic"></param>
        public static void ClearAssetBundle(string path, Dictionary<string, List<string>> bundleDic)
        {
            //进度条
            float min = ms_GenerateBuildInfoProgress.x;
            float max = ms_GenerateBuildInfoProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(ClearAssetBundle)}", "清除多余的AssetBundle文件", min);

            //获取path下的目录 然后hashSet设置不重复的 然后遍历Bundle的Key来逐步把多余的清空掉

            //首先获取buildPath的文件列表 然后根据该文件列表去清除 具体Bundle里的东西，这样BuildPath下是比较干净的

            List<string> fileList = GetFiles(path, null, null);//前后缀不设限制
            HashSet<string> fileSet = new HashSet<string>(fileList); //HashSet

            //取bundleDic
            foreach (string bundle in bundleDic.Keys)
            {
                fileSet.Remove($"{path}{bundle}");
                fileSet.Remove($"{path}{bundle}{BUNDLE_MANIFEST_SUFFIX}");//移除manifest文件？
            }

            fileSet.Remove($"{path}{PLATFORM}");//对应平台下的  都是没有用
            fileSet.Remove($"{path}{PLATFORM}{BUNDLE_MANIFEST_SUFFIX}");//这是？

            Parallel.ForEach(fileSet, parallelOptions, File.Delete);

            EditorUtility.DisplayProgressBar($"{nameof(ClearAssetBundle)}", "清除多余的AssetBundle文件", max);
        }


        /// <summary>
        /// 打包AssetBundle
        /// </summary>
        /// <returns></returns>
        public static AssetBundleManifest BuildBundle(Dictionary<string, List<string>> bundleDic)
        {
            //进度条
            float min = ms_BuildBundleProgress.x;
            float max = ms_BuildBundleProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(BuildBundle)}", "打包AssetBundle", min);

            //打包的位置
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(buildPath, GetBuilds(bundleDic),
                buildAssetBundleOptions, EditorUserBuildSettings.activeBuildTarget);

            EditorUtility.DisplayProgressBar($"{nameof(BuildBundle)}", "打包AssetBundle", max);

            return manifest;
        }

        private static void BuildManifest()
        {
            //进度条
            float min = ms_BuildManifestProgress.x;
            float max = ms_BuildManifestProgress.y;

            EditorUtility.DisplayProgressBar($"{nameof(BuildManifest)}", "将Manifest打包成AssetBundle", min);

            if (!Directory.Exists(TempBuildPath))
            {
                Directory.CreateDirectory(TempBuildPath);
            }

            //需要获取上层目录···？

            string prefix = Application.dataPath.Replace("/Assets", "/").Replace("\\", "/");

            AssetBundleBuild manifest = new AssetBundleBuild();
            manifest.assetBundleName = $"{MANIFEST}{BUNDLE_SUFFIX}";
            manifest.assetNames = new string[3]
            {
            ResourcePath_Binary.Replace(prefix,""),
            BundlePath_Binary.Replace(prefix,""),
            DependencyPath_Binary.Replace(prefix,""),
            };

            EditorUtility.DisplayProgressBar($"{nameof(BuildManifest)}", "将Manifest打包成AssetBundle", min + (max - min) * 0.5f);

            AssetBundleManifest assetBundleManifest = BuildPipeline.BuildAssetBundles(TempBuildPath,
                new AssetBundleBuild[] { manifest }, buildAssetBundleOptions, EditorUserBuildSettings.activeBuildTarget);
            //这些变量都不知道是什么hh

            //把文件copy进build目录
            if (assetBundleManifest)
            {
                string manifestFile = $"{TempBuildPath}/{MANIFEST}{BUNDLE_SUFFIX}";
                string target = $"{buildPath}/{MANIFEST}/{BUNDLE_SUFFIX}";
                if (File.Exists(manifestFile))
                {
                    File.Copy(manifestFile, target);
                }
            }

            //删除临时目录
            if (!Directory.Exists(TempBuildPath))
            {
                Directory.Delete(TempBuildPath, true);
            }

            EditorUtility.DisplayProgressBar($"{nameof(BuildManifest)}", "将Manifest打包成AssetBundle", max);

        }

        /// <summary>
        /// 获取所有需要打包的AssetBundleBuild
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
                    //定义名字
                    assetBundleName = pair.Key,
                    assetNames = pair.Value.ToArray()
                };
            }
            return assetBundleBuilds;
        }


        /// <summary>
        /// 7.获取指定路径的文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prefix"></param>
        /// <param name="suffies"></param>
        /// <returns></returns>
        public static List<string> GetFiles(string path, string prefix, params string[] suffixes)
        {
            //Directory.GetFiles的作用是获取path目录中的所有文件
            string[] files = Directory.GetFiles(path, $"*.*", SearchOption.AllDirectories);
            //根据文件数量
            List<string> result = new List<string>(files.Length);

            for (int i = 0; i < files.Length; i++)
            {
                //切换格式
                string file = files[i].Replace('\\', '/');

                //固定区域性单词比较规则？？
                //前缀？但视频说的是后缀？应该就是前缀
                if (prefix != null && file.StartsWith(prefix, StringComparison.InvariantCulture))
                {
                    continue;
                }

                if (suffixes != null && suffixes.Length > 0)
                {
                    //后缀存在吗?
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


