using QuestPatcher.Axml;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BonelabFusionPatch
{
    internal class ManifestEditor
    {
        internal const string clientId = "xyza7891gwlwvjx3rdlols6vj05u9jwt";

        private static readonly Uri AndroidNamespaceUri = new("http://schemas.android.com/apk/res/android");

        private const int NameAttributeResourceId = 16842755;

        internal static bool PatchManifest(string apkPath)
        {
            using FileStream zipStream = new(apkPath, FileMode.Open);
            using ZipArchive archive = new(zipStream, ZipArchiveMode.Read | ZipArchiveMode.Update);

            ZipArchiveEntry manifestEntry = archive.Entries.First(a => a.Name == "AndroidManifest.xml");
            using Stream manifestStream = manifestEntry.Open();
            using MemoryStream memoryStream = new();

            manifestStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            AxmlElement manifest = AxmlLoader.LoadDocument(memoryStream);

            AddEOSPermissions(manifest);
            AddClientId(manifest);

            using MemoryStream saveStream = new();
            AxmlSaver.SaveDocument(saveStream, manifest);
            saveStream.Position = 0;
            manifestStream.Position = 0;

            saveStream.CopyTo(manifestStream);

            return true;
        }

        private static void AddEOSPermissions(AxmlElement manifest)
        {
            string[] eosPermissions =
            {
                "android.permission.ACCESS_WIFI_STATE",
                "android.permission.DOWNLOAD_WITHOUT_NOTIFICATION",
            };

            HashSet<string> existingPermissions = GetExistingChildren(manifest, "uses-permission");

            foreach (string permission in eosPermissions)
            {
                if (existingPermissions.Contains(permission)) { continue; } // Do not add existing permissions

                AxmlElement permElement = new("uses-permission");
                AddNameAttribute(permElement, permission);
                manifest.Children.Add(permElement);
            }
        }

        private static void AddClientId(AxmlElement manifest)
        {
            AxmlElement application = manifest.Children.FirstOrDefault(e => e.Name == "application")!;
            AxmlElement activity = new("activity")
            {
                Attributes =
                {
                    new("configChanges", AndroidNamespaceUri, 16844002, "keyboardHidden|orientation|screenSize"),
                    new("exported", AndroidNamespaceUri, 16844000, "true"),
                    new("name", AndroidNamespaceUri, NameAttributeResourceId, "com.epicgames.mobile.eossdk.EOSAuthHandlerActivity"),
                    new("noHistory", AndroidNamespaceUri, 16844001, "true"),
                }
            };
            AxmlElement intentFilter = new("intent-filter");
            intentFilter.Children.Add(new("data")
            {
                Attributes = 
                {
                    new("scheme", AndroidNamespaceUri, 16842791, $"eos.{clientId.ToLower()}"),
                }
            });

            activity.Children.Add(intentFilter);
            application.Children.Add(activity);
        }

        private static void AddNameAttribute(AxmlElement element, string name)
        {
            element.Attributes.Add(new("name", AndroidNamespaceUri, NameAttributeResourceId, name));
        }

        private static HashSet<string> GetExistingChildren(AxmlElement manifest, string childNames)
        {
            HashSet<string> result = [];

            foreach (AxmlElement element in manifest.Children)
            {
                if (element.Name != childNames) { continue; }

                List<AxmlAttribute> nameAttributes = element.Attributes.Where(attribute => attribute.Namespace == AndroidNamespaceUri && attribute.Name == "name").ToList();
                // Only add children with the name attribute
                if (nameAttributes.Count > 0) { result.Add((string)nameAttributes[0].Value); }
            }

            return result;
        }
    }
}
