using System;
using GameSystem.GameCore.Physics;

namespace GameSystem.GameCore
{
    /// <summary>
    /// Basic collider component
    /// </summary>
    public abstract class Collider : Component
    {
        [DoNotClone]
        protected CollisionProxy colProxy;
        public delegate void OnCollisionHandler(Collider self, Collider other);
        public event OnCollisionHandler OnCollisionEvent;

        #region Mask properties
        private bool lockMask = false;
        private int _mask = -1;
        public int Mask
        {
            get { return _mask; }
            set
            {
                if (!lockMask)
                    _mask = value;
                else
                    throw new InvalidOperationException("Mask was been locked.");
            }
        }
        #endregion

        public override void Start()
        {
            colProxy.collider = this;
            colProxy.SetTransform(transform.matrix);
            colProxy.CollisionEvent += ColProxy_CollisionEvent;
            OnChangeExecutableEvent += Collider_ChangeExecutableEvent;
            lockMask = true;
        }

        private void Collider_ChangeExecutableEvent(bool active)
        {
            if (active)
            {
                //Log($"{Name}[{gameObject.SID}] added into phy. eng. with group = {Layer}, mask = {Mask}");
                Manager.PhysicEngine.AddCollision(colProxy, Layer, Mask);
            }
            else
            {
                //Log($"{Name}[{gameObject.SID}] remove from phy. eng.");
                Manager.PhysicEngine.RemoveCollision(colProxy);
            }
        }

        public bool IsEventNull()
        {
            return OnCollisionEvent == null;
        }

        public override void OnDestroy()
        {
            // remove collision proxy from physic engine
            Manager.PhysicEngine.RemoveCollision(colProxy);
        }

        public override void LateUpdate()
        {
            colProxy.SetTransform(transform.matrix);
        }

        private void ColProxy_CollisionEvent(CollisionProxy colA, CollisionProxy colB)
        {
            if (OnCollisionEvent != null)
            {
                if (colA.collider.executing && colB.collider.executing)
                {
                    OnCollisionEvent.Invoke(colA.collider, colB.collider);
                }
                else
                {
                    //Log($"{colA.collider.Name}[{colA.collider.gameObject.SID}] vs {colB.collider.Name}[{colB.collider.gameObject.SID}] Result : ");
                    //if (!colA.collider.executing)
                    //    Log($"{colA.collider.Name}[{colA.collider.gameObject.SID}] is not executing");
                    //if (!colB.collider.executing)
                    //    Log($"{colB.collider.Name}[{colB.collider.gameObject.SID}] is not executing");
                }
            }
            //else
            //    Log("In collider, event is null");
        }
    }

    

}
