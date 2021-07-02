﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Logging;

namespace Ubiq.Spawning
{
    public interface IUnspawnable
    {
        void Unspawn(bool remove);
    }
    public interface ISpawnable
    {
        NetworkId Id { set; }
        void OnSpawned(bool local);
    }

    public class NetworkSpawner : MonoBehaviour, INetworkObject, INetworkComponent
    {
        public NetworkId Id { get; } = new NetworkId("a369-2643-7725-a971");

        public RoomClient roomClient;
        public PrefabCatalogue catalogue;


        private NetworkContext context;
        private Dictionary<NetworkId, GameObject> spawned;
        private EventLogger events;

        [Serializable]
        public struct Message // public to avoid warning 0649
        {
            public int catalogueIndex;
            public NetworkId networkId;
            public bool remove;
        }

        private void Reset()
        {
            if(roomClient == null)
            {
                roomClient = GetComponentInParent<RoomClient>();
            }
#if UNITY_EDITOR
            if(catalogue == null)
            {
                try
                {
                    var asset = UnityEditor.AssetDatabase.FindAssets("Prefab Catalogue").FirstOrDefault();
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(asset);
                    catalogue = UnityEditor.AssetDatabase.LoadAssetAtPath<PrefabCatalogue>(path);
                }
                catch
                {
                    // if the default prefab has gone away, no problem
                }
            }
#endif
        }

        private void Awake()
        {
            spawned = new Dictionary<NetworkId, GameObject>();
        }

        void Start()
        {
            context = NetworkScene.Register(this);
            events = new ContextEventLogger(context);
            roomClient.OnRoom.AddListener(OnRoom);
        }

        private GameObject Instantiate(int i, NetworkId networkId, bool local)
        {
            var go = GameObject.Instantiate(catalogue.prefabs[i], transform);
            var spawnable = go.GetSpawnableInChildren();
            spawnable.Id = networkId;
            spawnable.OnSpawned(local);
            spawned[networkId] = go;
            events.Log("SpawnObject", i, networkId, local);
            return go;
        }

        public GameObject Spawn(GameObject gameObject)
        {
            var i = ResolveIndex(gameObject);
            var networkId = NetworkScene.GenerateUniqueId();
            context.SendJson(new Message() { catalogueIndex = i, networkId = networkId });
            return Instantiate(i, networkId, true);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            if (msg.remove)
            {
                Debug.Log("ProcessMessage: remove");
                var key = $"SpawnedObject-{ msg.networkId }";
                roomClient.Room[key] = JsonUtility.ToJson(new Message() {networkId = msg.networkId, remove = true});
                Destroy(spawned[msg.networkId]);
                spawned.Remove(msg.networkId);
            }
            else
            {
                Debug.Log("ProcessMessage");
                Instantiate(msg.catalogueIndex, msg.networkId, false);
            }
        }

        public GameObject SpawnPersistent(GameObject gameObject)
        {
            var i = ResolveIndex(gameObject);
            var networkId = NetworkScene.GenerateUniqueId();
            //Debug.Log("SpawnPersistent() " + networkId.ToString());
            var key = $"SpawnedObject-{ networkId }";
            var spawned = Instantiate(i, networkId, true);
            roomClient.Room[key] = JsonUtility.ToJson(new Message() { catalogueIndex = i, networkId = networkId });
            return spawned;
        }

        public void UnspawnPersistent(NetworkId networkId)
        {
            context.SendJson(new Message() { networkId = networkId, remove = true });
            var key = $"SpawnedObject-{ networkId }";
            roomClient.Room[key] = JsonUtility.ToJson(new Message() { networkId = networkId, remove = true });
            Destroy(spawned[networkId]);
            spawned.Remove(networkId);
            Debug.Log("UnspawnPersistent");
        }

        private void OnRoom(RoomInfo room)
        {
            foreach (var item in room.Properties)
            {
                if(item.Key.StartsWith("SpawnedObject"))
                {
                    Debug.Log(item.Key);
                    var msg = JsonUtility.FromJson<Message>(item.Value);
                    
                    if (!spawned.ContainsKey(msg.networkId))
                    {
                        Debug.Log("OnRoom");
                        if (!msg.remove)
                        {
                            Instantiate(msg.catalogueIndex, msg.networkId, false);
                        }
                        else
                        {
                        }
                    }
                }
            }
        }

        private int ResolveIndex(GameObject gameObject)
        {
            var i = catalogue.IndexOf(gameObject);
            Debug.Assert(i >= 0, $"Could not find {gameObject.name} in Catalogue. Ensure that you've added your new prefab to the Catalogue on NetworkSpawner before trying to instantiate it.");
            return i;
        }

        public static GameObject Spawn(MonoBehaviour caller, GameObject prefab)
        {
            var spawner = FindNetworkSpawner(NetworkScene.FindNetworkScene(caller));
            return spawner.Spawn(prefab);
        }

        public static GameObject SpawnPersistent(MonoBehaviour caller, GameObject prefab)
        {
            var spawner = FindNetworkSpawner(NetworkScene.FindNetworkScene(caller));
            return spawner.SpawnPersistent(prefab);
        }

        public static NetworkSpawner FindNetworkSpawner(NetworkScene scene)
        {
            var spawner = scene.GetComponentInChildren<NetworkSpawner>();
            Debug.Assert(spawner != null, $"Cannot find NetworkSpawner Component for {scene}. Ensure a NetworkSpawner Component has been added.");
            return spawner;
        }
    }

    public static class NetworkSpawnerExtensions
    {
        public static ISpawnable GetSpawnableInChildren(this GameObject gameObject)
        {
            return gameObject.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is ISpawnable).FirstOrDefault() as ISpawnable;
        }
    }


}