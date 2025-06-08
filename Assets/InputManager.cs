using MathiasCode;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class InputManager : MonoBehaviour
{
    [Serializable]
    public struct ControllerInput
    {
        public ControllerButton Button;
        public InputActionReference ActionRef;
        public string AxisAlias;
        internal InputAction InputAction;
    }

    public enum BlendMode
    {
        Or,
        Average
    }

    public enum ControllerHand 
    {
        Left,
        Right,
        Both
    }
    public static InputManager Instance { get; private set; }

    public List<ControllerInput> leftControllerInputs = new();
    public List<ControllerInput> rightControllerInputs = new();

    public PIDInteractionManager leftController;
    public PIDInteractionManager rightController;

    void Start()
    {
        Instance = this;
    }

    public PIDInteractionManager GetController(ControllerHand hand) => hand switch
    {
        ControllerHand.Left => leftController,
        ControllerHand.Right => rightController,
        _ => throw new InvalidOperationException("nuh uhh")
    };

    public float GetInput(ControllerButton btn, ControllerHand hand, BlendMode blend = BlendMode.Or)
    {
        List<ControllerInput> selectedList = hand switch
        {
            ControllerHand.Left => leftControllerInputs,
            ControllerHand.Right => rightControllerInputs,
            _ => new(leftControllerInputs.Concat(rightControllerInputs))
        };

        switch (blend)
        {
            case BlendMode.Average:
                float output = 0f;
                List<ControllerInput> inputs = selectedList.FindAll(c => c.Button == btn);
                foreach (var input in inputs)
                {
                    if (!string.IsNullOrWhiteSpace(input.AxisAlias)) 
                    {
                        output += Input.GetAxis(input.AxisAlias);
                        continue;
                    }
                    output += ReadInput(input);
                }
                output /= inputs.Count;
                return output;
            case BlendMode.Or:
            default:
                ControllerInput inp = selectedList.Find(c => c.Button == btn);
                if (!string.IsNullOrWhiteSpace(inp.AxisAlias)) return Input.GetAxis(inp.AxisAlias);
                return ReadInput(inp);
        }

        float ReadInput(ControllerInput input) 
        {
            if (input.InputAction == null) input.InputAction = input.ActionRef.action;
            return input.InputAction.ReadValue<float>();
        }
    }
}
