﻿using System.Runtime.InteropServices;

namespace ViewFaceCore.Plus
{
    /// <summary>
    /// 适用于 Any CPU 的 ViewFacePlus
    /// </summary>
    static partial class ViewFaceBridge
    {
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string path);

        /// <summary>
        /// 获取本机库目录
        /// </summary>
        private static string LibraryPath
        {
            get
            {
              
                string architecture = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X86 => "x86",
                    Architecture.X64 => "x64",
                    Architecture.Arm => "arm",
                    Architecture.Arm64 => "arm64",
                    _ => throw new PlatformNotSupportedException($"不支持的处理器体系结构: {RuntimeInformation.ProcessArchitecture}"),
                };
                string platform;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                { platform = "win"; }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                { platform = "linux"; }
                else
                { throw new PlatformNotSupportedException($"不支持的操作系统: {RuntimeInformation.OSDescription}"); }

                var libraryPath = Path.Combine(Environment.CurrentDirectory, "viewfacecore", platform, architecture);
                if (Directory.Exists(libraryPath))
                {
                    return libraryPath;
                }
                else { throw new DirectoryNotFoundException($"找不到本机库目录: {libraryPath}"); }
            }
        }

        /// <summary>
        /// ViewFaceBridge 的所有依赖库。(按照依赖顺序排列)
        /// </summary>
        private static readonly List<string> Libraries = new()
        {
            "SeetaAuthorize",
            "tennis",
            "tennis_haswell",
            "tennis_pentium",
            "tennis_sandy_bridge",
            "SeetaMaskDetector200",
            "SeetaAgePredictor600",
            "SeetaEyeStateDetector200",
            "SeetaFaceAntiSpoofingX600",
            "SeetaFaceDetector600",
            "SeetaFaceLandmarker600",
            "SeetaFaceRecognizer610",
            "SeetaFaceTracking600",
            "SeetaGenderPredictor600",
            "SeetaPoseEstimation600",
            "SeetaQualityAssessor300",
        };

        /// <summary>
        /// 在首次使用时初始化本机库目录。
        /// <para>贡献: <a href="https://github.com/withsalt">withsalt</a></para>
        /// <para>参考: <a href="https://docs.microsoft.com/en-us/dotnet/standard/native-interop/cross-platform">Cross Platform P/Invoke</a></para>
        /// <para></para>
        /// </summary>
        /// <exception cref="BadImageFormatException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="PlatformNotSupportedException"></exception>
        static ViewFaceBridge()
        {
#if NETFRAMEWORK || NETSTANDARD
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetDllDirectory(LibraryPath);
            }
#elif NETCOREAPP3_1_OR_GREATER
            #region Resolver Libraries on Linux
            // Author: <a href="https://github.com/withsalt">withsalt</a>
            // 预加载 ViewFaceBridge 的所有依赖库

            string format = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            { format = "{0}.dll"; }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            { format = "lib{0}.so"; }
            
            foreach (var library in Libraries)
            {
                string libraryPath = Path.Combine(LibraryPath, string.Format(format, library));
                if (File.Exists(libraryPath))
                {
                    if (NativeLibrary.Load(libraryPath) == IntPtr.Zero)
                    { throw new BadImageFormatException($"加载本机库失败: {library}"); }
                }
                else if(!libraryPath.Contains("tennis"))
                { throw new FileNotFoundException($"找不到本机库：{libraryPath}"); }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    libraryPath = Path.Combine(LibraryPath, string.Format("{0}d.dll", library));
                    if (File.Exists(libraryPath))
                    {
                        if (NativeLibrary.Load(libraryPath) == IntPtr.Zero)
                        { throw new BadImageFormatException($"加载本机库失败: {library}"); }
                    }
                    else if (!libraryPath.Contains("tennis"))
                    { throw new FileNotFoundException($"找不到本机库：{libraryPath}"); }
                }
            }

            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), (libraryName, assembly, searchPath) =>
            {
                var library = "ViewFaceBridge";
                if (libraryName.Equals(library, StringComparison.OrdinalIgnoreCase))
                {
                    string libraryPath = Path.Combine(LibraryPath, string.Format(format, library));
                    return NativeLibrary.Load(libraryPath, assembly, searchPath ?? DllImportSearchPath.ApplicationDirectory);
                }
                return IntPtr.Zero;
            });
            #endregion
#else
            throw new PlatformNotSupportedException($"不支持的 .NET 平台: {RuntimeInformation.FrameworkDescription}");
#endif
        }

    }
}
