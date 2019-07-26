using GameSystem.GameCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleGameServer.SimpleGame
{
    public class BattleAgent : Component
    {
        public int id;
        public int hp;

        public override void Start()
        {            //Collider collider;
            //if((collider = GetComponent<Collider>()) != null)
            //{
            //    collider.OnCollisionEvent += Collider_OnCollisionEvent;
            //}

        }

        private void Collider_OnCollisionEvent(Collider self, Collider other)
        {
            
        }
    }
}
