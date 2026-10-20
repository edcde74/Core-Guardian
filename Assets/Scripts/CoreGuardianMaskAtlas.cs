using System;
using System.Collections.Generic;
using UnityEngine;

// Dedicated mask pages; source sprites and their importers remain untouched.
public sealed class CoreGuardianMaskAtlas : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public Texture2D source;
        public Texture2DArray page;
        public int layer;
        public string dependencyHash;
    }
    [SerializeField] Entry[] entries=Array.Empty<Entry>();
    [SerializeField] string inputHash;
    Dictionary<Texture2D,Entry> lookup;
    public int Revision {get;private set;}
    public void Invalidate(){lookup=null;Revision++;}
    public void Initialize(Entry[] values,string hash){entries=values;inputHash=hash;Invalidate();}
    public bool TryGet(Texture2D source,out Entry entry)
    {
        if(lookup==null)
        {
            lookup=new Dictionary<Texture2D,Entry>();
            foreach(var value in entries)
            {
                if(value.source==null||value.page==null||value.page.width!=value.source.width||value.page.height!=value.source.height||value.layer<0||value.layer>=value.page.depth)continue;
                if(value.source.filterMode!=FilterMode.Point||value.source.wrapModeU!=TextureWrapMode.Clamp||value.source.wrapModeV!=TextureWrapMode.Clamp||value.source.mipmapCount!=1)continue;
#if UNITY_EDITOR
                // Validate once when building the cache, outside the rendering hot path.
                string path=UnityEditor.AssetDatabase.GetAssetPath(value.source);
                if(value.dependencyHash!=UnityEditor.AssetDatabase.GetAssetDependencyHash(path).ToString())continue;
#endif
                lookup[value.source]=value;
            }
        }
        if(source==null){entry=default;return false;}
        return lookup.TryGetValue(source,out entry);
    }
    void OnEnable()=>Invalidate();
    void OnValidate()=>Invalidate();
}
