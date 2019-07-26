using GameSystem.GameCore;
using GameSystem.GameCore.SerializableMath;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleGameServer.SimpleGame
{
    public class Bullet : Component
    {
        public int id;
        public Vector3 direction;
        public float speed = 10f;

        public int shooterId;

        public float remainTimer = 0f;
        public float remainTime = 3f;

        public BulletInfo GetInfo()
        {
            return new BulletInfo(id, transform.position);
        }

        public bool UpdateBullet(float deltaTime)
        {
            transform.position += direction * speed * deltaTime;
            //Log(transform.position);
            remainTimer += deltaTime;
            return remainTimer > remainTime;
        }

        public void Reset()
        {
            remainTimer = 0;
        }
    }
}
