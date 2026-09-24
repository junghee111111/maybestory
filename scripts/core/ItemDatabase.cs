using Godot;
using System.Collections.Generic;

// res://data/item 아래 모든 ItemData(.tres)를 ItemId 기준으로 조회할 수 있도록 스캔/캐싱한다.
public static class ItemDatabase
{
  private const string RootPath = "res://data/item";

  private static Dictionary<string, ItemData> _cache;

  public static ItemData Find(string itemId)
  {
    if (string.IsNullOrEmpty(itemId)) return null;

    EnsureCache();
    return _cache.TryGetValue(itemId, out var item) ? item : null;
  }

  private static void EnsureCache()
  {
    if (_cache != null) return;

    _cache = new Dictionary<string, ItemData>();
    ScanDirectory(RootPath);
  }

  private static void ScanDirectory(string path)
  {
    using var dir = DirAccess.Open(path);
    if (dir == null) return;

    dir.ListDirBegin();
    string entry = dir.GetNext();
    while (entry != "")
    {
      string fullPath = $"{path}/{entry}";
      if (dir.CurrentIsDir())
      {
        ScanDirectory(fullPath);
      }
      else if (entry.EndsWith(".tres"))
      {
        if (GD.Load(fullPath) is ItemData item && !string.IsNullOrEmpty(item.ItemId))
        {
          _cache[item.ItemId] = item;
        }
      }
      entry = dir.GetNext();
    }
    dir.ListDirEnd();
  }
}
