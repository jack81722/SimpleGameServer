using GameSystem;
using GameSystem.GameCore;
using GameSystem.GameCore.Components;
using GameSystem.GameCore.Network;
using GameSystem.GameCore.SerializableMath;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleGameServer.SimpleGame
{
    public class BulletManager : Component
    {
        private int hitCount = 0;

        private List<Bullet> bullets;
        private GameObject bulletPrefab;
        private BulletPool bulletPool;

        private IdentityPool bulletIdPool = new IdentityPool();

        public override void Start()
        {   
            bulletPrefab = CreateBulletPrefab();
            bulletPool = new BulletPool(bulletPrefab.GetComponent<Bullet>());
            bulletPool.Supple(5);
            bullets = new List<Bullet>();
        }


        /// <summary>
        /// Create bullet prefab
        /// </summary>
        /// <returns>bullet prefab game object</returns>
        public GameObject CreateBulletPrefab()
        {
            GameObject prefab = CreateGameObject();
            prefab.Name = "Bullet";
            Bullet component = prefab.AddComponent<Bullet>();
            SphereCollider collider = prefab.AddComponent<SphereCollider>();
            collider.SetSize(0.125f);
            collider.Layer = SimpleGameCollisionGroup.Bullet;
            collider.Mask = SimpleGameCollisionGroup.Character;
            //Log($"Bullet collider layer = {collider.Layer}, mask = {collider.Mask}");
            collider.OnCollisionEvent += Collider_OnCollisionEvent;
            //Log("Create bullet prefab. event is null? " + collider.IsEventNull());
            prefab.SetActive(false);
            return prefab;
        }

        private void Collider_OnCollisionEvent(Collider self, Collider other)
        {
            //Console.WriteLine($"Hit {other.gameObject.Name} {hitCount++}");
            Bullet bullet = self.GetComponent<Bullet>();
            BattleAgent agent = other.GetComponent<BattleAgent>();
            if (bullet != null && other.GetComponent< ServerSimpleBox>().id != bullet.shooterId)
            {
                Log($"Bullet[{bullet.SID}] recycled.");
                bulletPool.Recycle(bullet);
                bullets.Remove(bullet);
            }
            //if (bullet != null && agent != null && bullet.shooterId != agent.id)
            //{
            //    bulletPool.Recycle(bullet);
            //    bullets.Remove(bullet);
            //}
        }

        public override void Update()
        {
            float second = (float)DeltaTime.TotalSeconds;

            List<BulletInfo> bulletPacket = new List<BulletInfo>();
            for (int i = 0; i < bullets.Count; i++)
            {
                if (bullets[i].UpdateBullet(second))
                {
                    // if bullet timed out, recycle bullet
                    bulletPool.Recycle(bullets[i]);
                    bullets.RemoveAt(i--);
                }
                else
                {
                    // update bullet info into client
                    bulletPacket.Add(bullets[i].GetInfo());
                }
            }
            bulletPacket.Sort((x, y) => x.id.CompareTo(y.id));
            Network_Broadcast(
                new object[] { SimpleGameMetrics.ServerGameSwitchCode.BulletInfo, bulletPacket.ToArray() },
                Reliability.Sequence);
        }

        public void ShootBullet(int shooterId, Vector3 position, Vector3 direction)
        {
            try
            {
                Bullet bullet = bulletPool.Get(bulletIdPool.NewID());
                bullet.shooterId = shooterId;
                bullet.direction = direction;
                bullet.transform.position = position;
                bullets.Add(bullet);
            }
            catch (Exception e)
            {

                LogError(e);
            }
        }
        
    }

    public class BulletPool : TrackableObjectPool<Bullet>
    {
        private Bullet bulletPrefab;

        public BulletPool(Bullet prefab)
        {
            bulletPrefab = prefab;
        }

        protected override int Comparison(Bullet x, Bullet y)
        {
            return x.SID.CompareTo(y.SID);
        }

        protected override Bullet Create()
        {
            Bullet bullet = GameObject.Instantiate(bulletPrefab.gameObject).GetComponent<Bullet>();
            //Console.WriteLine("Create bullet. event is null? " + bullet.GetComponent<SphereCollider>().IsEventNull());
            return bullet;
        }

        protected override void SuppleHandler(Bullet item)
        {
            item.gameObject.SetActive(false);
        }

        protected override void GetHandler(Bullet item, object arg)
        {
            item.gameObject.SetActive(true);
            DefaultDebugger.GetInstance().Log(item.SID);
            item.id = (int)arg;
        }

        protected override void RecycleHandler(Bullet item)
        {
            item.gameObject.SetActive(false);
            item.Reset();
        }

        protected override void Destroy(Bullet item)
        {
            GameObject.Destroy(item.gameObject);
        }
    }
}
