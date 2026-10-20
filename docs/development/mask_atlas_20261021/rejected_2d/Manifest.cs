using System;
using System.Collections.Generic;
using UnityEngine;

// Dedicated mask pages; source sprites and their importers remain untouched.
public sealed class CoreGuardianMaskAtlas : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public Texture2D source,page;
        public RectInt rect;
        public Vector2 Map(Vector2 uv)=>new Vector2((rect.x+uv.x*source.width)/page.width,(rect.y+uv.y*source.height)/page.height);
    }
    [SerializeField] Entry[] entries=Array.Empty<Entry>();
    [SerializeField] string inputHash;
    Dictionary<Texture2D,Entry> lookup;
    public void Initialize(Entry[] values,string hash){entries=values;inputHash=hash;lookup=null;}
    public bool TryGet(Texture2D source,out Entry entry)
    {
        if(lookup==null)
        {
            lookup=new Dictionary<Texture2D,Entry>();
            foreach(var value in entries)
                if(value.source!=null&&value.page!=null&&value.rect.width==value.source.width&&value.rect.height==value.source.height)
                    lookup[value.source]=value;
        }
        if(source==null){entry=default;return false;}
        return lookup.TryGetValue(source,out entry);
    }
    void OnEnable()=>lookup=null;
    void OnValidate()=>lookup=null;
}
