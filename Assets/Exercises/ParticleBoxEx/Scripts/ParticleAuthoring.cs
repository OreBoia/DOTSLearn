using Unity.Entities;
using UnityEngine;

namespace Exercises.ParticleBoxEx.Scripts
{
    public class ParticleAuthoring : MonoBehaviour
    {
        private class ParticleAuthoringBaker : Baker<ParticleAuthoring>
        {
            public override void Bake(ParticleAuthoring authoring)
            {
            }
        }
    }
}