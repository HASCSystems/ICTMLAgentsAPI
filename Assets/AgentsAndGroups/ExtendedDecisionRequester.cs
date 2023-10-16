using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.MLAgents
{
    public class ExtendedDecisionRequester : DecisionRequester
    {
        /// <summary>
        /// The frequency with which the agent requests a decision. A DecisionPeriod of 5 means
        /// that the Agent will request a decision every 5 Academy steps. /// </summary>
        [Range(1, 200)]
        [Tooltip("The frequency with which the agent requests a decision. A DecisionPeriod " +
            "of 5 means that the Agent will request a decision every 5 Academy steps.")]
        public int OverrideDecisionPeriod = 5;

        /// <summary>
        /// Whether Agent.RequestDecision should be called on this update step.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected override bool ShouldRequestDecision(DecisionRequestContext context)
        {
            return context.AcademyStepCount % OverrideDecisionPeriod == 0;
        }
    }
}