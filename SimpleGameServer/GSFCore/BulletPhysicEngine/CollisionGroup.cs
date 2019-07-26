using BulletSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace BulletEngine
{
    public class CollisionGroup
    {
        public const int None = (int)CollisionFilterGroups.None;
        public const int All = (int)CollisionFilterGroups.AllFilter;
        public const int Default = (int)CollisionFilterGroups.DefaultFilter;
        public const int Static = (int)CollisionFilterGroups.StaticFilter;
        public const int Kinematic = (int)CollisionFilterGroups.KinematicFilter;
        public const int Debris = (int)CollisionFilterGroups.DebrisFilter;
        public const int Sensor = (int)CollisionFilterGroups.SensorTrigger;
        public const int Character = (int)CollisionFilterGroups.CharacterFilter;
    }
}
