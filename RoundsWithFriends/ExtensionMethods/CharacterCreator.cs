using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RWF
{
    public class CharacterCreatorAdditionalData
    {
        public List<GameObject> objectsToEnable;
    }

    public static class CharacterCreatorExtensions
    {
        private static readonly ConditionalWeakTable<CharacterCreator, CharacterCreatorAdditionalData> additionalData = new ConditionalWeakTable<CharacterCreator, CharacterCreatorAdditionalData>();

        public static CharacterCreatorAdditionalData GetData(this CharacterCreator instance) {
            return additionalData.GetOrCreateValue(instance);
        }

        /* In the base game, there are only two character selection instances in local multiplayer lobby. When a character is being modified,
         * it opens the CharacterCreator and disables the character selection instance so that the instance and creator aren't drawn at the same
         * time on top of each other. To make this work, CharacterCreator keeps track of the game object (which is the character selection instance)
         * it needs to enable when the creator is closed.
         * 
         * Alas, the CharacterCreator keeps track of only one game object, so we need to define our own data structure for keeping track of multiple
         * game objects since we have more than two character selection instances.
         */
        public static void SetObjectsToEnable(this CharacterCreator instance, List<GameObject> objects) {
            instance.GetData().objectsToEnable = objects;
        }

        public static List<GameObject> GetObjectsToEnable(this CharacterCreator instance) {
            return instance.GetData().objectsToEnable ?? new List<GameObject>();
        }
    }
}
