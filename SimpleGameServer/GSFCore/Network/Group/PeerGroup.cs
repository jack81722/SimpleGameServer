﻿using GameSystem.GameCore.Debugger;
using LiteNetLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameSystem.GameCore.Network
{
    /// <summary>
    /// Group of stack packets and send 
    /// </summary>
    public class PeerGroup : IPeerGroup
    {
        private static IdentityPool idPool = new IdentityPool();
        public int GroupId { get; private set; }
        public virtual int OperationCode { get; set; }
        protected ISerializer serializer;
        protected IDebugger debugger;

        /// <summary>
        /// Peers in group
        /// </summary>
        protected Dictionary<int, IPeer> peers;

        #region Join request fields
        /// <summary>
        /// Queue of requests want to join
        /// </summary>
        private List<JoinGroupRequest> joinQueuing;
        /// <summary>
        /// List of requests are handling but not respond yet
        /// </summary>
        private List<JoinGroupRequest> joinHandling;
        #endregion

        protected PacketEventPool eventPool;
        private bool isPolling;
        private bool isClosed = false;

        public delegate void OnPeerJoinedHandler(IPeer peer);
        public delegate void OnPeerExitedHandler(IPeer peer);

        public OnReceivePacketHandler OnGroupReceiveEvent;
        public OnPeerJoinedHandler OnPeerJoinedEvent;
        public OnPeerExitedHandler OnPeerExitedEvent;

        public PeerGroup(ISerializer serializer, IDebugger debugger)
        {
            GroupId = idPool.NewID();
            this.serializer = serializer;
            this.debugger = debugger;
            peers = new Dictionary<int, IPeer>();
            eventPool = new PacketEventPool();
            joinQueuing = new List<JoinGroupRequest>();
            joinHandling = new List<JoinGroupRequest>();
        }

        public void Poll()
        {
            PacketEvent[] events = eventPool.DequeueAll();
            isPolling = true;
            for (int i = 0; i < events.Length; i++)
            {
                IPeer peer = events[i].GetPeer();
                object data = events[i].GetData();
                Reliability reliability = events[i].GetReliability();
                OnGroupReceiveEvent.Invoke(peer, data, reliability);    // delegate method
            }
            isPolling = false;
            if (isClosed)
                CloseSafely();
        }

        #region Send methods
        public void Broadcast(object obj, Reliability reliability)
        {
            GenericPacket packet = new GenericPacket();
            packet.InstCode = OperationCode;
            packet.Data = obj;
            byte[] bytes = serializer.Serialize(packet);
            foreach (var peer in peers.Values)
            {
                peer.Send(bytes, reliability);
            }
        }

        public async Task BroadcastAsync(object obj, Reliability reliability)
        {   
            Task task = Task.Run(() => Broadcast(obj, reliability));
            await task;
        }

        public void Send(int peerID, object obj, Reliability reliability)
        {
            if (peers.TryGetValue(peerID, out IPeer peer))
            {
                GenericPacket packet = new GenericPacket();
                packet.InstCode = OperationCode;
                packet.Data = obj;
                byte[] bytes = serializer.Serialize(packet);
                peer.Send(bytes, reliability);
            }
        }

        public async Task SendAsync(int peerId, object obj, Reliability reliability)
        {
            GenericPacket packet = new GenericPacket();
            packet.InstCode = OperationCode;
            packet.Data = obj;
            Task task = Task.Run(() => Send(peerId, packet, reliability));
            await task;
        }
        #endregion

        public void AddEvent(IPeer peer, object data, Reliability reliability)
        {
            if(!isClosed)
                eventPool.Enqueue(peer, data, reliability);
        }

        #region Join/Exit request methods
        public async Task<JoinGroupResponse> JoinAsync(IPeer peer, object arg)
        {
            // check if group is closed
            if (isClosed)
                return new JoinGroupResponse(GroupId, OperationCode, JoinGroupResponse.ResultType.Cancelled, string.Format("Group is closed."));

            // check if peer has joined
            if (peers.ContainsKey(peer.Id))
                return new JoinGroupResponse(GroupId, OperationCode, JoinGroupResponse.ResultType.HasJoined, string.Format("Has joined in group"));

            // check if peer is in queue
            if (joinQueuing.Exists(req => req.Peer.Id == peer.Id))
                return new JoinGroupResponse(GroupId, OperationCode, JoinGroupResponse.ResultType.InQueue, string.Format("Join request is in queue."));

            // check if peer is been handling
            if(joinHandling.Exists(req => req.Peer.Id == peer.Id))
                return new JoinGroupResponse(GroupId, OperationCode, JoinGroupResponse.ResultType.Handling, string.Format("Join request is handling."));

            // add request into queue and waiting result
            JoinGroupRequest request = new JoinGroupRequest(GroupId, OperationCode, peer, arg);
            joinQueuing.Add(request);
            JoinGroupResponse result = await request.Task;
            joinHandling.Remove(request);       // remove request from handling (because finished)
            if (result.type == JoinGroupResponse.ResultType.Accepted)
            {
                lock (peers) peers.Add(peer.Id, peer);
                peer.TrackGroup(this);
                try
                {
                    if (OnPeerJoinedEvent != null)
                        OnPeerJoinedEvent.Invoke(peer);
                }
                catch (Exception e)
                {
                    debugger.LogError(e);
                }
            }
            return result;
        }

        /// <summary>
        /// Search and cancel join request from specific list
        /// </summary>
        /// <param name="reqList">request list</param>
        /// <param name="peer">peer of cancellation</param>
        /// <param name="msg">cancel message</param>
        /// <param name="arg">cancel argument</param>
        /// <returns>success of cancellation</returns>
        private bool CancelRequestFromList(ref List<JoinGroupRequest> reqList, IPeer peer, string msg = "", object arg = null)
        {
            JoinGroupRequest request;
            lock (reqList)
            {   
                if ((request = reqList.Find(req => req.Peer == peer)) != null)
                {
                    request.Cancel(msg, arg);
                    reqList.Remove(request);
                    return true;
                }
                else
                {
                    // means not found
                    return false;
                }
            }
        }

        /// <summary>
        /// Cancel all of join request from specific list
        /// </summary>
        /// <param name="reqList">request list</param>
        /// <param name="msg">cancel message</param>
        /// <param name="arg">cancel argument</param>
        /// <returns>success of cancellation</returns>
        private int CancelAllRequestFromList(ref List<JoinGroupRequest> reqList, string msg = "", object arg = null)
        {
            lock (reqList)
            {
                int reqCount = reqList.Count;
                foreach(var request in reqList)
                {
                    request.Cancel(msg, arg);
                }
                reqList.Clear();
                return reqCount;
            }
        }

        public int GetJoinQueueingCount()
        {
            return joinQueuing.Count;
        }

        public int GetJoinHandlingCount()
        {
            return joinHandling.Count;
        }

        public JoinGroupRequest DequeueJoinRequest()
        {
            lock (joinQueuing)
            {
                if (joinQueuing.Count < 0)
                    throw new InvalidOperationException("No queueing request.");
                JoinGroupRequest request = joinQueuing[0];
                joinQueuing.RemoveAt(0);
                // add request into handling
                joinHandling.Add(request);
                return request;
            }
        }

        private Queue<ExitGroupEvent> exitEventQueue = new Queue<ExitGroupEvent>();

        public void Exit(IPeer peer, object arg = null)
        {
            string msg = "Exited group.";
            CancelRequestFromList(ref joinQueuing, peer, msg, arg);
            CancelRequestFromList(ref joinHandling, peer, msg, arg);
            lock (peers)
            {
                peers.Remove(peer.Id);
                peer.UntrackGroup(this);
            }
            ExitGroupEvent e = new ExitGroupEvent(GroupId, peer, arg);
            exitEventQueue.Enqueue(e);
        }

        public void ExitAll(string msg = "", object arg = null)
        {
            // cancel all of queuing requests
            int queueingCount =
                CancelAllRequestFromList(ref joinQueuing, msg, arg);
            // cancel all of handling requests
            int handlingCount =
                CancelAllRequestFromList(ref joinHandling, msg, arg);
            
            int inGroupCount = peers.Count;
            // untrack current group from peers
            List<IPeer> inGroups = new List<IPeer>(peers.Values);
            // clear all peers
            peers.Clear();
            foreach (var peer in inGroups)
            {
                peer.UntrackGroup(this);
            }
            debugger.Log($"Group[{GroupId}] exit all peers, queueing cancelled : {queueingCount}, handling cancelled : {handlingCount}, in group : {inGroupCount}");
        }

        public List<ExitGroupEvent> GetExitEventList()
        {
            List<ExitGroupEvent> list = new List<ExitGroupEvent>(exitEventQueue);
            exitEventQueue.Clear();
            return list;
        }
        #endregion

        #region Search peer methods
        /// <summary>
        /// Get peer in group by peer identity
        /// </summary>
        /// <exception cref="Peer not found in group."></exception>
        public IPeer GetPeer(int peerID)
        {
            if (peers.TryGetValue(peerID, out IPeer peer))
            {
                return peer;
            }
            throw new InvalidOperationException("Peer not found in group.");
        }

        /// <summary>
        /// Try to get peer in group by peer identity
        /// </summary>
        public bool TryGetPeer(int peerID, out IPeer peer)
        {
            return peers.TryGetValue(peerID, out peer);
        }

        /// <summary>
        /// Get peer list in group
        /// </summary>
        public List<IPeer> GetPeerList()
        {
            return new List<IPeer>(peers.Values);
        }

        /// <summary>
        /// Find all peers matched by predicate
        /// </summary>
        public List<IPeer> FindAllPeers(Predicate<IPeer> predicate)
        {
            lock (peers)
            {
                List<IPeer> found = new List<IPeer>(peers.Values);
                return found.FindAll(predicate);
            }
        }

        public bool ContainsPeer(int peerID)
        {
            return peers.ContainsKey(peerID);
        }

        public bool ContainsPeer(IPeer peer)
        {
            return peers.ContainsKey(peer.Id);
        }
        #endregion

        public void Close()
        {
            isClosed = true;
            if (!isPolling)
                CloseSafely();
        }

        private void CloseSafely()
        {
            ExitAll("Group is closed.", null);
            eventPool.Clear();
        }
    }

    public class ExitGroupEvent
    {
        public int groupId;
        public IPeer peer;
        public object arg;

        public ExitGroupEvent(int groupId, IPeer peer, object arg)
        {
            this.groupId = groupId;
            this.peer = peer;
            this.arg = arg;
        }
    }
}