using System;
using SG.Dialogue.Enums;
using UnityEngine;

namespace SG.Dialogue.Events
{
    [Serializable]
    public struct ParticleRequest
    {
        public ParticleActionType ActionType;
        public string ParticleID;
        public GameObject Prefab;
        public Vector3 Position;
        public Vector3 Scale;

        public ParticleRequest(ParticleActionType actionType, string particleID, GameObject prefab, Vector3 position, Vector3 scale)
        {
            ActionType = actionType;
            ParticleID = particleID;
            Prefab = prefab;
            Position = position;
            Scale = scale;
        }
    }
}
