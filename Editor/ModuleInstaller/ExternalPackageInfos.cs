using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace NSEUtils.PackageUtils.ModuleInstaller
{

    [Serializable]
    internal class ExternalPackageInfos
    {
        public string packageName;
        [SerializeField] string githubUrl;

        public string version;

        public string packageUrl => $"{githubUrl}#v{version}";



        FullPackageVersion m_fullVersion;
        public FullPackageVersion fullVersion
        {
            get
            {
                //Debug.Log($"m_fullVersion is null:{m_fullVersion is null} | {version}");
                if (m_fullVersion is null)
                    m_fullVersion = new FullPackageVersion(version);
                return m_fullVersion;
            }
        }
        public ExternalPackageInfos()
        {

        }
        public ExternalPackageInfos(UnityEditor.PackageManager.PackageInfo packageInfo)
        {
            string _json = JsonUtility.ToJson(packageInfo);
            JObject pkg_JSON = JObject.Parse(_json);
            string fullUrl = pkg_JSON["m_ProjectDependenciesEntry"].ToString();
            string[] urlParsed = fullUrl.Split('#');

            githubUrl = urlParsed[0];
            if (urlParsed.Length > 1)
                version = urlParsed[1];
            else
                version = pkg_JSON["m_Version"].ToString();

            packageName = packageInfo.name;
        }
        public ExternalPackageInfos(string _packageName, string _githubUrl, string _version)
        {
            packageName = _packageName;
            githubUrl = _githubUrl;
            version = _version;
        }

    }

    internal class FullPackageVersion
    {
        public int major, minor, patch;
        public VersionChannel patchChannel;
        public int pre_channelVersion;

        internal enum VersionChannel
        {
            undefined = -1,
            pre,
            alpha,
            beta,
            rc,
            release,
        }

        public FullPackageVersion()
        {

        }
        public FullPackageVersion(string _version)
        {
            if (string.IsNullOrEmpty(_version)) return;
            //Debug.Log(_version);

            string[] M_m_p = _version.Replace("v", "").Split('.');
            if (M_m_p.Length >= 3)
            {
                int.TryParse(M_m_p[0].Substring(0, 1), out major);
                int.TryParse(M_m_p[1].Substring(0, 1), out minor);
                int.TryParse(M_m_p[2].Substring(0, 1), out patch);
                patchChannel = VersionChannel.release;

                if (M_m_p.Length == 4 && M_m_p[2].Contains('-'))
                {
                    var splitPatch = M_m_p[2].Split('-');

                    if (splitPatch.Length > 1)
                    {
                        if (!Enum.TryParse(splitPatch[1], true, out patchChannel) || !int.TryParse(M_m_p[3], out pre_channelVersion))
                        {
                            patchChannel = VersionChannel.undefined;
                            pre_channelVersion = 0;
                        }
                    }

                    //Debug.Log($" [{splitPatch.Length}] {string.Join('|', splitPatch)} ({patchChannel})");
                }

            }
            else
                Debug.LogError($"Fail on translate Pakage version : got {(M_m_p != null ? M_m_p.Length : -1)}. espected 3 or more ");
        }

        public override string ToString()
        {
            if (patchChannel > 0 && patchChannel < VersionChannel.release)
                return $"v{major}.{minor}.{patch}-{patchChannel.ToString()}.{pre_channelVersion}";
            else if (patchChannel == VersionChannel.release)
                return $"v{major}.{minor}.{patch}";
            else
                return $"{VersionChannel.undefined.ToString()}";

        }

        public static bool operator <(FullPackageVersion left, FullPackageVersion right)
        {
            if (left.major != right.major) return left.major < right.major;
            if (left.minor != right.minor) return left.minor < right.minor;
            if (left.patch != right.patch) return left.patch < right.patch;
            if (left.patchChannel != right.patchChannel) return left.patchChannel < right.patchChannel;
            return left.pre_channelVersion < right.pre_channelVersion;
        }

        public static bool operator >(FullPackageVersion left, FullPackageVersion right)
        {
            if (left.major != right.major) return left.major > right.major;
            if (left.minor != right.minor) return left.minor > right.minor;
            if (left.patch != right.patch) return left.patch > right.patch;
            if (left.patchChannel != right.patchChannel) return left.patchChannel > right.patchChannel;
            return left.pre_channelVersion > right.pre_channelVersion;
        }

        public static bool operator ==(FullPackageVersion left, FullPackageVersion right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;

            return left.major == right.major &&
                   left.minor == right.minor &&
                   left.patch == right.patch &&
                   left.patchChannel == right.patchChannel &&
                   left.pre_channelVersion == right.pre_channelVersion;
        }

        public static bool operator !=(FullPackageVersion left, FullPackageVersion right) => (ReferenceEquals(left, null) || ReferenceEquals(right, null)) || !(left == right);
        public static bool operator >=(FullPackageVersion left, FullPackageVersion right) => left > right || left == right;
        public static bool operator <=(FullPackageVersion left, FullPackageVersion right) => left < right || left == right;

        public override bool Equals(object obj)
        {
            if (obj is FullPackageVersion otherVersion)
                return this == otherVersion;
            return false;
        }

        public override int GetHashCode()
        {
            return (major, minor, patch, patchChannel, pre_channelVersion).GetHashCode();
        }
    }

}
