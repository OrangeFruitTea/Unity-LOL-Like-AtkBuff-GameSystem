using System;
using UnityEngine;

namespace Basement.ResourceManagement
{
    [Serializable]
    public class ResourcePoolConfig
    {
        public string ResourcePath;
        public int InitialCapacity = 10;
        public int MaxCapacity = 100;
        public bool PreloadOnStart = false;
    }
}