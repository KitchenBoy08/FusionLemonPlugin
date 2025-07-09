using QuestPatcher.Axml;
using System.IO.Compression;

namespace BonelabFusionPatch;

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
        AddEOSAuthActivity(manifest);
        AddEOSApplicationAttributes(manifest);
        AddAndroidXProvider(manifest);

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
            if (existingPermissions.Contains(permission)) { continue; }

            AxmlElement permElement = new("uses-permission");
            AddNameAttribute(permElement, permission);
            manifest.Children.Add(permElement);
        }
    }

    private static void AddEOSAuthActivity(AxmlElement manifest)
    {
        AxmlElement application = manifest.Children.FirstOrDefault(e => e.Name == "application")!;
        AxmlElement activity = new("activity");

        activity.Attributes.Add(new("configChanges", AndroidNamespaceUri, 16842783, 0x04A0));
        activity.Attributes.Add(new("exported", AndroidNamespaceUri, 16842768, "true"));
        activity.Attributes.Add(new("name", AndroidNamespaceUri, NameAttributeResourceId, "com.epicgames.mobile.eossdk.EOSAuthHandlerActivity"));
        activity.Attributes.Add(new("noHistory", AndroidNamespaceUri, 16843309, "true"));

        AxmlElement intentFilter1 = new("intent-filter");
        intentFilter1.Children.Add(new("action")
        {
            Attributes = 
            {
                new("name", AndroidNamespaceUri, NameAttributeResourceId, "android.intent.action.VIEW"),
            }
        });
        intentFilter1.Children.Add(new("category")
        {
            Attributes = 
            {
                new("name", AndroidNamespaceUri, NameAttributeResourceId, "android.intent.category.DEFAULT"),
            }
        });
        intentFilter1.Children.Add(new("category")
        {
            Attributes = 
            {
                new("name", AndroidNamespaceUri, NameAttributeResourceId, "android.intent.category.BROWSABLE"),
            }
        });
        intentFilter1.Children.Add(new("data")
        {
            Attributes = 
            {
                new("scheme", AndroidNamespaceUri, 0x01010027, $"eos.{clientId.ToLower()}"),
            }
        });

        activity.Children.Add(intentFilter1);

        if (application.Children.Any(e => e.Name == "activity" && e.Attributes.Any(a => (string)a.Value == "com.epicgames.mobile.eossdk.EOSAuthHandlerActivity")))
            application.Children.RemoveAll(e => e.Name == "activity" && e.Attributes.Any(a => (string)a.Value == "com.epicgames.mobile.eossdk.EOSAuthHandlerActivity"));
        application.Children.Add(activity);
    }

    private static void AddEOSApplicationAttributes(AxmlElement manifest)
    {
        AxmlElement application = manifest.Children.FirstOrDefault(e => e.Name == "application")!;

        if (application.Attributes.Where(e => e.Name == "appComponentFactory").FirstOrDefault() != null)
            application.Attributes.Add(new("appComponentFactory", AndroidNamespaceUri, 16844154, "androidx.core.app.CoreComponentFactory"));
    }

    private static void AddAndroidXProvider(AxmlElement manifest)
    {
        AxmlElement application = manifest.Children.FirstOrDefault(e => e.Name == "application")!;

        if (application.Children.Where(e => e.Name == "provider").Any(e => e.Attributes.Any(x => (string)x.Value == "com.StressLevelZero.BONELAB.androidx-startup")))
            return;

        AxmlElement provider = new("provider")
        {
            Attributes =
            {
                new("authorities", AndroidNamespaceUri, 0x01010018, "com.StressLevelZero.BONELAB.androidx-startup"),
                new("exported", AndroidNamespaceUri, 16842768, "false"),
                new("name", AndroidNamespaceUri, NameAttributeResourceId, "androidx.startup.InitializationProvider"),
            }
        };

        AddMetaData(provider, "androidx.emoji2.text.EmojiCompatInitializer", "androidx.startup");
        AddMetaData(provider, "androidx.lifecycle.ProcessLifecycleInitializer", "androidx.startup");
        AddMetaData(provider, "androidx.lifecycle.ReportFragmentInitializer", "androidx.startup");

        application.Children.Add(provider);

        application.Children.Add(new("uses-library")
        {
            Attributes =
            {
                new("name", AndroidNamespaceUri, NameAttributeResourceId, "androidx.window.extensions"),
                new("required", AndroidNamespaceUri, 16843406, "false"),
            }
        });

        application.Children.Add(new("uses-library")
        {
            Attributes =
            {
                new("name", AndroidNamespaceUri, NameAttributeResourceId, "androidx.window.sidecar"),
                new("required", AndroidNamespaceUri, 16843406, "false"),
            }
        });

        AxmlElement receiver = new("receiver")
        {
            Attributes =
            {
                new("directBootAware", AndroidNamespaceUri, 0x01010505, "false"),
                new("enabled", AndroidNamespaceUri, 0x0101000e, "true"),
                new("exported", AndroidNamespaceUri, 16842768, "true"),
                new("name", AndroidNamespaceUri, NameAttributeResourceId, "androidx.profileinstaller.ProfileInstallReceiver"),
                new("permission", AndroidNamespaceUri, 0x01010006, "android.permission.DUMP"),
            }
        };

        string[] actions = {
            "androidx.profileinstaller.action.INSTALL_PROFILE",
            "androidx.profileinstaller.action.SKIP_FILE",
            "androidx.profileinstaller.action.SAVE_PROFILE",
            "androidx.profileinstaller.action.BENCHMARK_OPERATION"
        };

        foreach (string action in actions)
        {
            AxmlElement intentFilter = new("intent-filter");
            intentFilter.Children.Add(new("action")
            {
                Attributes =
                {
                    new("name", AndroidNamespaceUri, NameAttributeResourceId, action),
                }
            });
            receiver.Children.Add(intentFilter);
        }

        application.Children.Add(receiver);
    }

    private static void AddMetaData(AxmlElement element, string name, string value)
    {
        if (element.Children.Any(e => e.Name == "meta-data" && e.Attributes.Any(e => e.Name == name)))
            element.Children.RemoveAll(e => e.Name == "meta-data" && e.Attributes.Any(a => a.Name == name));

        AxmlElement metaData = new("meta-data")
        {
            Attributes =
            {
                new("name", AndroidNamespaceUri, NameAttributeResourceId, name),
                new("value", AndroidNamespaceUri, 0x01010024, value),
            }
        };
        element.Children.Add(metaData);
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
