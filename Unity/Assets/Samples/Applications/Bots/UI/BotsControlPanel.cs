﻿using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Samples.Bots;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ubiq.Samples.Bots.UI
{
    public class BotsControlPanel : MonoBehaviour
    {
        public BotsController Controller;
        public Text CommandRoomJoinCodeLabel;
        public Text NumberOfPeersLabel;
        public Button CreateRoomButton;
        public Button JoinRoomButton;
        public Button ToggleCameraButton;
        public InputField JoinCodeInputField;
        public Toggle EnableAudioToggle;
        public Camera Camera;
        public LayoutGroup BotManagerControlList;
        public Button ClearButton;

        // VR2022 Controls
        public InputField UpdateRateField;
        public InputField PaddingField;

        private GameObject BotManagerControlPrototype;
        private List<BotsManagerControl> BotManagerControls;

        private void Awake()
        {
            CreateRoomButton.onClick.AddListener(() =>
            {
                Controller.CreateBotsRoom();
            });

            JoinRoomButton.onClick.AddListener(() =>
            {
                Controller.AddBotsToRoom(JoinCodeInputField.text);
            });

            ClearButton.onClick.AddListener(() =>
            {
                Controller.ClearBots();
            });

            ToggleCameraButton.onClick.AddListener(ToggleCamera);

            EnableAudioToggle.onValueChanged.AddListener(Controller.ToggleAudio);

            BotManagerControls = new List<BotsManagerControl>();
            BotManagerControlPrototype = BotManagerControlList.transform.GetChild(0).gameObject;
            BotManagerControlPrototype.SetActive(false);

            Controller.EnableAudio = EnableAudioToggle.isOn;
        }

        // Update is called once per frame
        void Update()
        {
            if(!Controller)
            {
                return;
            }

            NumberOfPeersLabel.text = Controller.NumBotsRoomPeers.ToString();

            if (!JoinCodeInputField.isFocused)
            {
                JoinCodeInputField.text = Controller.BotsJoinCode;
            }
            CommandRoomJoinCodeLabel.text = Controller.CommandJoinCode;

            while (BotManagerControls.Count < Controller.BotManagers.Count)
            {
                BotManagerControls.Add(
                    GameObject.Instantiate(BotManagerControlPrototype, BotManagerControlList.transform)
                    .GetComponent<BotsManagerControl>()
                );
            }

            int i = 0;
            foreach (var item in Controller.BotManagers)
            {
                BotManagerControls[i].SetProxy(item);
                BotManagerControls[i].gameObject.SetActive(true);
                i++;
            }

            try
            {
                Controller.UpdateRate = int.Parse(UpdateRateField.text);
            }
            catch
            {
            }

            try
            {
                Controller.Padding = int.Parse(PaddingField.text);
            }
            catch
            {
            }
        }

            public void ToggleCamera()
        {
            Camera.cullingMask ^= 1 << LayerMask.NameToLayer("Default");
        }
    }
}