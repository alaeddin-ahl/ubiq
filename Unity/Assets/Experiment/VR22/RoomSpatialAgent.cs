﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Rooms.Spatial;
using UnityEngine;

namespace Ubiq.Rooms.Spatial
{
    /// <summary>
    /// Controls room membership and observation based on the player's current position
    /// </summary>
    [RequireComponent(typeof(RoomClient))]
    public class RoomSpatialAgent : MonoBehaviour
    {
        /// <summary>
        /// The transform most accurately representing the players position. This should the GameObject that defines the camera, or similar, for example.
        /// </summary>
        public Transform Player;

        private RoomClient roomClient;
        private SpatialState state;

        public string Shard;

        private Guid member;
        private List<Guid> observed;

        private void Awake()
        {
            roomClient = GetComponent<RoomClient>();
            state = new SpatialState();
            state.Shard = Guid.Parse(Shard);
            observed = new List<Guid>();

        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            var partition = SpatialPartition.ScenePartition;
            if(partition == null)
            {
                return;
            }

            partition.GetRooms(Player.position, state);

            if(member != state.Member)
            {
                member = state.Member;
                roomClient.Join(member);
            }

            if(!roomClient.JoinedRoom)
            {
                return; // The spatial client must join one room before observing any others, for the peer to be set up correctly
            }

            int numExists = 0;
            foreach (var item in state.Observed)
            {
                if(observed.Contains(item))
                {
                    numExists++;
                }
            }

            if(!(state.Observed.Count == numExists && state.Observed.Count == observed.Count))
            {
                observed.Clear();
                observed.AddRange(state.Observed);
                SetObserved(observed);
            }
        }

        private struct SetObservedRequest
        {
            public List<string> rooms;
        }

        /// <summary>
        /// Observes the Rooms with the specified Guids. Stops observing any not in the list, and begins observing any new ones.
        /// </summary>
        void SetObserved(List<Guid> guids)
        {
            roomClient.SendToServer(
                new Messages.Message(
                    "SetObserved", 
                    new SetObservedRequest()
                    {
                        rooms = guids.Select(g => g.ToString()).ToList() // todo: GC
                    })
                );
        }
    }
}