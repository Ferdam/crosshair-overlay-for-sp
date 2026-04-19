using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrosshairOverlay.Models;

namespace CrosshairOverlay.Services;

public class ProfileService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CrosshairOverlay");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "profiles.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public ObservableCollection<Profile> Profiles { get; } = new();
    public Profile? Active { get; set; }

    public void Load()
    {
        Profiles.Clear();
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var store = JsonSerializer.Deserialize<ProfileStore>(json, JsonOptions);
                if (store != null)
                {
                    foreach (var p in store.Profiles) Profiles.Add(p);
                    Active = Profiles.FirstOrDefault(p => p.Name == store.ActiveProfileName) ?? Profiles.FirstOrDefault();
                }
            }
        }
        catch
        {
            // Corrupt file — fall through to defaults
        }

        if (Profiles.Count == 0)
        {
            var def = new Profile { Name = "Default" };
            def.Crosshair.Layers.Add(VectorLayer.CreateDefault(LayerPrimitive.Cross));
            Profiles.Add(def);
            Active = def;
        }

        // Ensure every vector profile has at least one layer so the UI has something to show
        foreach (var p in Profiles)
        {
            if (p.Crosshair.Mode == CrosshairMode.Vector && p.Crosshair.Layers.Count == 0)
            {
                p.Crosshair.Layers.Add(VectorLayer.CreateDefault(LayerPrimitive.Cross));
            }
        }

        Active ??= Profiles.First();
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        var store = new ProfileStore
        {
            Profiles = Profiles.ToList(),
            ActiveProfileName = Active?.Name,
        };
        var json = JsonSerializer.Serialize(store, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }
}
