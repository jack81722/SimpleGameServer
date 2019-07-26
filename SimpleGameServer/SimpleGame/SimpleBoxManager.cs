using GameSystem;
using GameSystem.GameCore;
using GameSystem.GameCore.Components;
using GameSystem.GameCore.Network;
using GameSystem.GameCore.SerializableMath;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SimpleGameServer.SimpleGame
{
    public class SimpleBoxManager : Component
    {
        private Dictionary<int, ServerSimpleBox> boxes;
        private Vector3 spawnPoint = new Vector3(0, 0, 0);
        private GameObject boxPrefab;

        BulletManager bulletMgr;

        public override void Start()
        {
            bulletMgr = FindObjectOfType<BulletManager>();

            OnReceiveGamePacket += OnReceiveControlPacket;
            boxPrefab = CreateBoxPrefab();
            boxes = new Dictionary<int, ServerSimpleBox>();

            var target = CreateAvatar(-1);
            target.Name = "TargetA";
            boxes.Add(target.id, target);
            target.transform.position = new Vector3(5, 0, 0);
            target = CreateAvatar(-2);
            target.Name = "TargetB";
            boxes.Add(target.id, target);
            target.transform.position = new Vector3(5, 0.5f, 0);

            HandleJoinRequest();
        }

        /// <summary>
        /// Create box prefab
        /// </summary>
        /// <returns>box prefab game object</returns>
        public GameObject CreateBoxPrefab()
        {
            GameObject prefab = CreateGameObject();
            prefab.Name = "Box";
            // add simple box component
            ServerSimpleBox box = prefab.AddComponent<ServerSimpleBox>();
            box.moveSpeed = 3f;
            BattleAgent agent = prefab.AddComponent<BattleAgent>();
            agent.hp = 5;

            // add collider component
            BoxCollider collider = prefab.AddComponent<BoxCollider>();
            collider.SetSize(new Vector3(0.25f));
            collider.Layer = SimpleGameCollisionGroup.Character;
            collider.Mask = SimpleGameCollisionGroup.Bullet;
            prefab.SetActive(false);
            return prefab;
        }

        private void HandleJoinRequest()
        {
            var joinReqs = Network_GetJoinRequests();
            for (int i = 0; i < joinReqs.Length; i++)
            {
                AcceptPlayer(joinReqs[i]);
            }
        }

        private void HandleExitEvent()
        {
            var exitEvents = Network_GetExitGroupEvents();
            for (int i = 0; i < exitEvents.Length; i++)
            {
                int boxId = exitEvents[i].peer.Id;
                if (boxes.TryGetValue(boxId, out ServerSimpleBox box))
                {
                    // destroy player avater
                    Destroy(box);
                    boxes.Remove(boxId);
                }
            }
            // for each exit join request ...
            // remove box from manager
            // remove all bullet which exited player shot?
        }

        /// <summary>
        /// Accept player
        /// </summary>
        /// <param name="request">join request</param>
        private void AcceptPlayer(JoinGroupRequest request)
        {
            IPeer peer = request.Accept("SimpleGame");
            // check if peer is connected
            if (peer.isConnected)
            {
                var box = CreateAvatar(peer.Id);
                boxes.Add(box.id, box);
                //Log("Player id = " + box.id);
            }
        }

        private ServerSimpleBox CreateAvatar(int id)
        {
            GameObject go = Instantiate(boxPrefab);
            go.SetActive(true);
            ServerSimpleBox box = go.GetComponent<ServerSimpleBox>();
            box.id = id;
            BattleAgent agent = go.GetComponent<BattleAgent>();
            agent.id = id;
            return box;
        }

        public float[] ToFloatArray(Vector3 v)
        {
            return new float[] { v.x, v.y, v.z };
        }

        public Vector3 ToVector3(float[] floats)
        {
            return new Vector3(floats[0], floats[1], floats[2]);
        }

        /// <summary>
        /// Receive packet handler method
        /// </summary>
        /// <param name="peer">source peer of sending the packet</param>
        /// <param name="packet">packet</param>
        public void OnReceiveControlPacket(IPeer peer, object packet)
        {
            object[] gamePacket = packet as object[];
            if (gamePacket != null)
            {
                int switchCode = (int)gamePacket[0];
                switch (switchCode)
                {
                    case SimpleGameMetrics.ClientGameSwitchCode.Move:
                        MoveControl(peer, (float[])gamePacket[1]);
                        break;
                    case SimpleGameMetrics.ClientGameSwitchCode.Shoot:
                        ShootControl(peer);
                        break;
                }
            }
        }

        public override void Update()
        {
            float second = (float)DeltaTime.TotalSeconds;
            int posIndex = 0;
            // player existed
            if (boxes.Count > 0)
            {
                BoxInfo[] boxPacket = new BoxInfo[boxes.Count];
                foreach (var box in boxes.Values)
                {
                    boxPacket[posIndex] = box.UpdatePosAndRot(second);
                    posIndex++;
                }

                Array.Sort(boxPacket, (x, y) => x.boxId.CompareTo(y.boxId));
                Network_Broadcast(
                    new object[] { SimpleGameMetrics.ServerGameSwitchCode.BoxInfo, boxPacket },
                    Reliability.Sequence);
            }

            HandleJoinRequest();
            HandleExitEvent();
        }

        /// <summary>
        /// Move control handler
        /// </summary>
        private void MoveControl(IPeer peer, float[] direction)
        {
            Vector3 d = ToVector3(direction);
            if (boxes.TryGetValue(peer.Id, out ServerSimpleBox box))
            {
                box.InputRotation = d.x;
                box.InputMove = d.z;
            }
        }

        private void ShootControl(IPeer peer)
        {
            if (boxes.TryGetValue(peer.Id, out ServerSimpleBox box))
            {   
                // get bullet from bullet manager
                bulletMgr.ShootBullet(peer.Id, box.transform.position + box.Direction, box.Direction);
            }
        }

    }



    

    
}