using System;
using System.Collections.Generic;
using ClearMerge.Utils;

namespace ClearMerge.Assets
{
    [Serializable]
    public class AssetConflict
    {
        public string assetPath;
        public string assetName;
        public string beforeContent;
        public string afterContent;
        public List<PropertyConflict> propertyConflicts = new List<PropertyConflict>();
        public bool isResolved = false;

        public AssetConflict(string path)
        {
            assetPath = path;
            assetName = System.IO.Path.GetFileName(path);
        }
    }
}
