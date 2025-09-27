using System;
using System.Collections.Generic;
using UnityEngine;
using ClearMerge.Utils;

namespace ClearMerge.Scenes
{


    /// <summary>
    /// Data class to represent component conflicts in scenes
    /// </summary>
    [Serializable]
    public class ComponentConflict
    {
        public string Name { get; private set; }

        public string Id { get; private set; }
        public List<PropertyConflict> ChangedProperties { get; private set; } = new();

        public List<CrossReferenceConflict> CrossReferences { get; private set; } = new();

        public ComponentConflict(string componentName, string componentId)
        {
            Id = componentId;
            Name = componentName;
        }
    }

    [Serializable]
    public class GameObjectConflict
    {
        public string Id { get; private set; }
        public string GlobalId { get; private set; } // GlobalObjectId for this GameObject
        public string Name { get; private set; }
        public GameObject BeforeVersion { get; set; }
        public GameObject AfterVersion { get; set; }

        public ConflictChoice Choice { get; set; } = ConflictChoice.None;
        public List<ComponentConflict> ComponentConflicts { get; private set; } = new();

        public GameObjectConflict(string id, string globalId, string name)
        {
            Id = id;
            GlobalId = globalId;
            Name = name;
        }

    }

    [Serializable]
    public class CrossReferenceConflict
    {
        public enum CrossReferenceConflictType
        {
            ParentChanged,
            TargetChanged,
            DependencyChanged,
            EventListenerChanged,
            CustomReferenceChanged
        }

        public CrossReferenceConflictType ConflictType { get; private set; }
        public string BeforeReferenceId { get; private set; }
        public string AfterReferenceId { get; private set; }

        public CrossReferenceConflict(
            CrossReferenceConflictType type,
            string beforeId,
            string afterId)
        {
            ConflictType = type;
            BeforeReferenceId = beforeId;
            AfterReferenceId = afterId;
        }
    }


}