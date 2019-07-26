using GameSystem.GameCore.Components;
using SimpleGameServer;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GameSystem.GameCore
{
    /// <summary>
    /// Component container that manager all component in game object
    /// </summary>
    public class ComponentContainer
    {
        private GameObject owner;
        private List<Component> components;
        private List<Component> temp_remove;

        #region Constructor
        public ComponentContainer(GameObject owner)
        {
            this.owner = owner;
            components = new List<Component>();
            temp_remove = new List<Component>();
        }
        #endregion

        #region Add/Remove methods
        /// <summary>
        /// Add new component into container and register to gs manager
        /// </summary>
        /// <typeparam name="T">component type</typeparam>
        /// <param name="gsMgr">game source manager</param>
        /// <returns>added component</returns>
        public T AddComponent<T>(GameSourceManager gsMgr) where T : Component, new()
        {
            lock (components)
            {
                // create component and initialize with gsMgr and SID
                T component = gsMgr.Create<T>();
                component.gameObject = owner;
                component.transform = owner.transform;
                components.Add(component);
                return component;
            }
        }

        /// <summary>
        /// Add specific component container
        /// </summary>
        /// <typeparam name="T">component type</typeparam>
        /// <param name="component">adding component</param>
        /// <returns>added component</returns>
        public T AddComponent<T>(T component) where T : Component
        {
            lock (components)
            {
                // create component and initialize with gsMgr and SID
                component.gameObject = owner;
                component.transform = owner.transform;
                components.Add(component);
                return component;
            }
        }

        /// <summary>
        /// Remove component
        /// </summary>
        public void RemoveComponent(Component component)
        {
            lock (components)
            {
                // save component temporarily and then remove on end of frame
                temp_remove.Add(component);
            }
        }
        #endregion

        #region Search methods
        /// <summary>
        /// Get all components
        /// </summary>
        public Component[] GetAllComponents()
        {
            return components.ToArray(); ;
        }

        /// <summary>
        /// Get component by specific type
        /// </summary>
        public T GetComponent<T>() where T : Component
        {
            Component c = components.Find(component => component.GetType() == typeof(T));
            if (c != null)
                return (T)c;
            return null;
        }

        /// <summary>
        /// Get all components which match specific type
        /// </summary>
        public Component[] GetComponents<T>() where T : Component
        {
            List<Component> c = components.FindAll(component => component.GetType() == typeof(T));
            if (c != null)
                return c.ToArray(); ;
            return new Component[0];
        }
        #endregion

        /// <summary>
        /// Copy all datas of components to target container
        /// </summary>
        /// <param name="container">target container</param>
        public void CopyTo(ref ComponentContainer container)
        {
            Component component;
            Type type;
            // clone every component
            for (int i = 0; i < components.Count; i++)
            {
                component = components[i];
                type = component.GetType();
                Component clone = (Component)Activator.CreateInstance(type);
                clone.SID = container.owner.Manager.NewSID();
                clone.Manager = container.owner.Manager;
                clone.gameObject = container.owner;
                container.owner.Manager.AddGameSource(clone);
                container.AddComponent(clone);

                //if (type == typeof(BoxCollider) || type == typeof(SphereCollider) || type == typeof(Collider))
                //{
                //    Console.WriteLine(type);
                //}

                    // clone fields
                FieldInfo[] fInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int f = 0; f < fInfos.Length; f++)
                {

                    if (!Attribute.IsDefined(fInfos[f], typeof(DoNotCloneAttribute)))
                    {
                        object value = fInfos[f].GetValue(component);

                        fInfos[f].SetValue(clone, value);

                    }
                }
                clone.transform = clone.gameObject.transform;

                // clone properties
                PropertyInfo[] pInfos = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int p = 0; p < pInfos.Length; p++)
                {
                    try
                    {
                        if (!Attribute.IsDefined(pInfos[p], typeof(DoNotCloneAttribute)))
                            if (pInfos[p].CanWrite && pInfos[p].CanRead)
                                pInfos[p].SetValue(clone, pInfos[p].GetValue(component));
                    }
                    catch (Exception e)
                    {
                        DefaultDebugger.GetInstance().LogError(e);
                        DefaultDebugger.GetInstance().Log($"Property : {clone.GetType()}.{pInfos[p].PropertyType} {pInfos[p].Name}");
                    }
                }

                EventInfo[] eInfos = type.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int e = 0; e < eInfos.Length; e++)
                {   
                    EventInfo eInfo = eInfos[e];
                    Console.WriteLine(eInfo);
                    if (!eInfo.IsDefined(typeof(DoNotCloneAttribute)))
                    {
                        var f = ReflectionTool.ReflectionTool.SearchFieldInfo(
                            type,
                            eInfo.Name,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        Delegate d = f.GetValue(component) as Delegate;
                        if (d != null)
                        {
                            Delegate[] invocations = d.GetInvocationList();

                            foreach (var invocation in invocations)
                            {
                                if (invocation.Method.DeclaringType == type)
                                {
                                    Delegate cloneDelegate = Delegate.CreateDelegate(type, invocation.Method);
                                    eInfo.AddEventHandler(clone, cloneDelegate);
                                }
                                else
                                {
                                    eInfo.AddEventHandler(clone, invocation);
                                }
                            }
                        }
                        //IEnumerable<MethodInfo> methods = ReflectionTool.ReflectionTool.GetSubscribedMethods(type, eInfo, component);
                        //foreach (var method in methods)
                        //{
                        //    if (method.DeclaringType == type)
                        //    {
                        //        Delegate cloneDelegate = Delegate.CreateDelegate(type, method);
                        //        eInfo.AddEventHandler(clone, cloneDelegate);
                        //    }
                        //    else
                        //    {
                        //        Delegate d = type.GetField(eInfo.Name).GetValue(component) as Delegate;
                        //        Delegate[] invocations = d.GetInvocationList();
                        //        eInfo.AddEventHandler(clone, delegate)
                        //    }
                        //}
                    }
                }

                // add component into clone
                //DefaultDebugger.GetInstance().Log($"Clone [{component}, SID = {component.SID}] by SID = {clone.SID} into GameObject[{clone.gameObject.SID}]");

                container.owner.Manager.AddGameSource(clone);
            }
        }

        /// <summary>
        /// Refresh add/remove components
        /// </summary>
        public void Refresh()
        {
            for(int i = 0; i< temp_remove.Count; i++)
            {
                components.Remove(temp_remove[i]);
            }
            temp_remove.Clear();
        }

        /// <summary>
        /// Clear all components
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < components.Count; i++)
                GameSource.Destroy(components[i]);
            temp_remove.AddRange(components);
        }
    }
}
