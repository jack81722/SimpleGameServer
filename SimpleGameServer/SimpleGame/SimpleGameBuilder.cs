using GameSystem.GameCore;
using GameSystem.GameCore.Components;
using GameSystem.GameCore.Debugger;
using GameSystem.GameCore.SerializableMath;
using System.Collections;
using System.Collections.Generic;

namespace SimpleGameServer.SimpleGame
{
    public class SimpleGameBuilder : SceneBuilder
    {
        public SimpleGameBuilder(IDebugger debugger) : base(debugger) { }

        protected override void Building()
        {
            GameObject box_manager = CreateGameObject("Simple Box Manager");
            box_manager.AddComponent<SimpleBoxManager>();

            GameObject bullet_manager = CreateGameObject("bullet_manager");
            bullet_manager.AddComponent<BulletManager>();
        }
    }
}