﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using AgarIO.Entities;
using AgarIO.Actions;
using System.Timers;

namespace AgarIO
{
    class Game
    {
        public static ServerConnection ServerConnection;
        LoginManager LoginManager;
        GraphicsEngine GraphicsEngine;
        InputManager InputManager;
        GameState GameState;
        string PlayerName;
        Timer GameTimer;

        public const int MaxLocationX = 2000;
        public const int MaxLocationY = 2000;
        const int GameLoopInterval = 60;


        /// <summary>
        /// Used for avoiding multiple game closes.
        /// </summary>
        public bool IsRunning { get; private set; }

        public void Init(LoginManager loginManager, GraphicsEngine graphicsEngine, 
            InputManager inputManager, ServerConnection connection, string playerName)
        {
            this.LoginManager = loginManager;
            this.GraphicsEngine = graphicsEngine;
            this.InputManager = inputManager;
            ServerConnection = connection;
            this.PlayerName = playerName;
        }

        public void Start()
        {
            GraphicsEngine.StartGraphics();
            ServerConnection.StartReceiving(OnReceiveMessage);
            IsRunning = true;
            StartLoop();
        }

        private async Task StartLoop()
        {
            //Task.Factory.StartNew(new System.Action(() => Loop2()));
            
            GameTimer = new Timer();
            GameTimer.Interval = GameLoopInterval;
            GameTimer.Elapsed += Loop;
            GameTimer.Start();
            
        }

        private void Loop(object sender, ElapsedEventArgs e)
        {
            if (GameState != null)
            {
                new MovementAction(InputManager.MousePosition).Process(GameState);
                GraphicsEngine.Render(GameState);
            }
        }

        private void Loop2()
        {
            var a = Stopwatch.GetTimestamp();
            while (true)
            {
                var b = Stopwatch.GetTimestamp();
                var delta = 1000 * (b - a) / Stopwatch.Frequency;
                if (GameState != null && delta > GameLoopInterval)
                {
                    new MovementAction(InputManager.MousePosition).Process(GameState);
                    GraphicsEngine.Render(GameState);
                    a = Stopwatch.GetTimestamp();
                }
            }
        }

        private void OnReceiveMessage(string msg)
        {
            var tokens = msg.Split();
            //Debug.WriteLine($"MSG: {msg}");
            switch (tokens[0])
            {
                case "STOP":
                    Close(msg.Substring(5));
                    break;
                default:       // it might be serialized game state
                    TryLoadState(msg);
                    break;
            }
        }

        public void TryLoadState(string msg)
        {
            byte[] data = Encoding.Default.GetBytes(msg);
            MemoryStream stream = new MemoryStream(data);
            try
            {
                var state = Serializer.Deserialize<GameState>(stream);
                this.GameState = state;
                state.CurrentPlayer = state.Players.Find(p => p.Name == PlayerName);
                // TODO - server has to add player to the state!
                //this.GameState.CurrentPlayer = State.Players.Find(p => p.Name == PlayerName);
                //Debug.WriteLine("Received new state!");
            } catch (SerializationException ex)
            {
                Debug.WriteLine("Deserializing error.");
            } catch (ArgumentNullException ex)
            {
                Debug.WriteLine("Couldn't find the current player in the current game state");
                Close("Error");
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("Game State is null after deserialization");
                Close("Error");
            }
        }

        public void Close(string msg)
        {
            IsRunning = false;
            GameTimer.Stop();
            ServerConnection.SendAsync("STOP").ContinueWith(new Action<Task>(t => {
                ServerConnection.Dispose();
            }));

            GraphicsEngine.StopGraphics();
            LoginManager.Show(msg); 
        }
    }
}
