using System.IO;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor.Compilation;
using System.Threading.Tasks;


namespace NSEUtils.PackageUtils.ModuleInstaller
{
    [Serializable]
    internal class CoreModuleSettings
    {
        public List<ExternalPackageInfos> dependencies;
    }

    [InitializeOnLoad]
    public class ModuleInstaller
    {
        [SerializeField] ExternalPackageInfos m_dependency;


        private static AddAndRemoveRequest addRequest;

        public static ListRequest InstalledPackagesSeachRequest;

        private static readonly string dependenciesFileName = "coremodulesettings.json";

        static ModuleInstaller()
        {
            //CompilationPipeline.compilationStarted -= OnAssemblyCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;

            //CompilationPipeline.compilationStarted += OnAssemblyCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            //Tst();
            CheckCoreDependencies();
            HandleResolveExternalDependencies();
        }

        //private static void OnAssemblyCompilationStarted(object obj)
        //{
        //    ManualResolveExternalDependencies();
        //    CompilationPipeline.compilationStarted -= OnAssemblyCompilationStarted;

        //}
        private static void OnAssemblyCompilationFinished(string assemblyName, CompilerMessage[] messages)
        {
            if (assemblyName.CompareTo("com.nseutils.packageutils") != 0) return;
            else if( !ExternalRegisteringPackages.Contains(assemblyName) )return;

            Debug.Log($"OnAssemblyCompilationFinished:{assemblyName}");
            bool hasErrors = false;
            // Verifica se houve erros na compilacao
            foreach (var message in messages)
            {
                if (message.type == CompilerMessageType.Error)
                {
                    hasErrors = true;
                    break;
                }
            }
            HandleResolveExternalDependencies(hasErrors);
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

        }

        static void ForceResolveExternalDependencies() => HandleResolveExternalDependencies(true);

        [MenuItem("Tools/NSEUtils/Verify external Package Dependencies")]
        static void ResolveExternalDependencies() => HandleResolveExternalDependencies();
        static async void HandleResolveExternalDependencies(bool forceResolve = false)
        {
            if (forceResolve)
            {
                Debug.Log($"FORCED Loading Dependencies...");
                var pckgs = Client.List();

                while (pckgs != null && !pckgs.IsCompleted)
                {
                    await Task.Delay(1);
                }
                CheckDependecyPackages(pckgs);
            }
            else
                CheckCoreDependencies();
            //Debug.Log($" Loading Dependencies...");

            //_CheckDependencies().Start();
        }

        //[InitializeOnLoadMethod]
        static void CheckCoreDependencies()
        {
            InstalledPackagesSeachRequest = Client.List();
            EditorApplication.update -= OnEditorStateUpdated;
            EditorApplication.update += OnEditorStateUpdated;

            EditorApplication.delayCall -= OnEditorStateUpdated;
            EditorApplication.delayCall += OnEditorStateUpdated;

            Events.registeringPackages -= OnRegisteringPackages;
            Events.registeringPackages += OnRegisteringPackages;
            //EditorApplication.update += OnEditorStateUpdated;
            //EditorApplication.update -= OnEditorStateUpdated;

        }
        static List<string> ExternalRegisteringPackages = new List<string>();
        private static void OnRegisteringPackages(PackageRegistrationEventArgs args)
        {
            //Debug.Log($"OnRegisteringPackages");
            foreach (var addedPackage in args.added)
            {
               if (addedPackage.source== PackageSource.Git)
               {
                   Debug.Log($"Installing Git Package on path: {addedPackage.resolvedPath}");
                    HandleResolveExternalDependencies(true);
                    ExternalRegisteringPackages.Add(addedPackage.name);
               }
               //Debug.Log($"OnRegisteringPackages.added:{added.name}");

            }

        }

        static void InstallPackages(string[] packagesToAdd)
        {
            addRequest = Client.AddAndRemove(packagesToAdd);

            EditorApplication.update += OnEditorStateUpdated;
        }
        static Dictionary<string, ExternalPackageInfos> installedPackages = new Dictionary<string, ExternalPackageInfos>();


        internal static void CheckDependecyPackages(ListRequest packages)
        {
            installedPackages = new Dictionary<string, ExternalPackageInfos>();
            List<string> packagesToInstallOrUpdate = new();


            if (packages.Status == StatusCode.Success)
                foreach (UnityEditor.PackageManager.PackageInfo installedPackage in packages.Result)
                {
                    bool _add = (installedPackage.source >= PackageSource.Embedded && !installedPackages.ContainsKey(installedPackage.name));
                    //Debug.Log($"CheckDependecyPackages : {installedPackage.name} found <color={(_add?"green":"red")}>({installedPackage.source.ToString()})</color>");

                    if (!_add) continue;

                    installedPackages.Add(installedPackage.name, new(installedPackage));
                }
            else
            {
                Debug.LogError(packages.Status);
                return;
            }
            packages = null;

            foreach (var installedPackage in installedPackages.Values)
            {
                string dependenciesFilePath = $"Packages/{installedPackage.packageName}/{dependenciesFileName}";
                if (!File.Exists(dependenciesFilePath))
                {
                    //Debug.LogError($"Package {installedPackage.packageName} has no settings");

                    return;
                }

                string dep_json = File.ReadAllText(dependenciesFilePath);
                //Debug.Log($"Checking Dependencies of {installedPackage.packageName}");
                if (dep_json != null)
                {
                    CoreModuleSettings settings = JsonUtility.FromJson<CoreModuleSettings>(dep_json);
                    foreach (ExternalPackageInfos requestedPackage in settings.dependencies)
                    {
                        //Debug.Log($"checking dependency {requestedPackage.packageName}");

                        if (IsPackageInstalled(requestedPackage.packageName) && IsPackageUpToDate(installedPackage, requestedPackage))
                            continue;
                        packagesToInstallOrUpdate.Add(requestedPackage.packageUrl);
                    }
                    //addRequest = null;
                    //Debug.Log($"Dependencies checked");

                    //if (addRequest != null)
                    //    Debug.Log($"(addRequest:{addRequest.Status})");

                    if (packagesToInstallOrUpdate.Count > 0)
                    {
                        Debug.Log($"INstalling {packagesToInstallOrUpdate.Count} Packages.");
                        InstallPackages(packagesToInstallOrUpdate.ToArray());
                    }
                    else
                    {
                        EditorApplication.update -= OnEditorStateUpdated;

                        EditorApplication.delayCall -= OnEditorStateUpdated;
                        //Debug.Log($"Core and his Dependencies is up to Date.");

                    }


                }
                else
                {
                    Debug.LogError("Falha ao carregar o dependecias.");
                }
                ExternalRegisteringPackages = new();
            }
        }

        internal static bool IsPackageInstalled(string packageName)
        {
            //FullPackageVersion requestedVersion = new();

            bool r = installedPackages.ContainsKey(packageName);
            //Debug.Log($"IsPackageInstalled:{r}");
            return r;
        }

        internal static bool IsPackageUpToDate(ExternalPackageInfos installedPackage, ExternalPackageInfos requestedVersion)
        {
            if (installedPackage is null || requestedVersion is null) throw new NullReferenceException($"requestedVersion is null:{requestedVersion is null} | installedPackage is null:{installedPackage is null}");
            //Debug.Log($"{installedPackage.packageName}#{installedPackage.fullVersion} installed. up to date :<b>{installedPackage.fullVersion >= requestedVersion.fullVersion}</b> requestedVersion:({requestedVersion.fullVersion})");
            return installedPackage.fullVersion >= requestedVersion.fullVersion;
        }

        private static void OnEditorStateUpdated()
        {
            if (InstalledPackagesSeachRequest != null && InstalledPackagesSeachRequest.IsCompleted)
            {
                CheckDependecyPackages(InstalledPackagesSeachRequest);
                InstalledPackagesSeachRequest = null;

            }
            if (addRequest != null && addRequest.IsCompleted)
            {
                if (addRequest.Status == StatusCode.Success)
                    Debug.Log("Pacotes instalados.");
                else if (addRequest.Status >= StatusCode.Failure)
                    Debug.LogError("Erro ao instalar o pacote: " + addRequest.Error.message);
                addRequest = null;
                EditorApplication.update -= OnEditorStateUpdated;
            }
        }
    }
}