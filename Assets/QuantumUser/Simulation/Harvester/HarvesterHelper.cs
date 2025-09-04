using System.Collections;
using System.Collections.Generic;
using Quantum.BotSDK;
using UnityEngine;

namespace Quantum
{
    public static unsafe class HarvesterHelper
    {
        public static void SetupAsBot(Frame frame, EntityRef entity)
        {
            // // Create the HFSM Agent and pick the AIConfig, if there is any
            // var hfsmAgent = new HFSMAgent();
            // var hfsmRoot = frame.FindAsset<HFSMRoot>(frame.RuntimeConfig.CollectorsSampleConfig.ReplacementHFSM.Id);
            // HFSMManager.Init(frame, &hfsmAgent.Data, entity, hfsmRoot);
            // frame.Set(entity, hfsmAgent);

            // // Setup the blackboard
            // var blackboardComponent = new AIBlackboardComponent();
            // var blackboardAsset = frame.FindAsset<AIBlackboard>(frame.RuntimeConfig.CollectorsSampleConfig.ReplacementAIBlackboard.Id);
            // var blackboardInitializerAsset = frame.FindAsset<AIBlackboardInitializer>(blackboardAsset.InitializerRef);
            // AIBlackboardInitializer.InitializeBlackboard(frame, &blackboardComponent, blackboardInitializerAsset);
            // frame.Set(entity, blackboardComponent);

            // // // Add the Bot component to store AI relevant data
            // // frame.Set(entity, default(Bot));

            // BotSDKDebuggerSystem.AddToDebugger(frame, entity, hfsmAgent);
        }
    }
}
