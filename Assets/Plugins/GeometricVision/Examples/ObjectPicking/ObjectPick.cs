// GENERATED AUTOMATICALLY FROM 'Assets/Plugins/GeometricVision/Examples/ObjectPicking/ObjectPick.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @ObjectPick : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @ObjectPick()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""ObjectPick"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""052784fc-eaa6-4aca-a686-7c5180586063"",
            ""actions"": [
                {
                    ""name"": ""Pick"",
                    ""type"": ""Button"",
                    ""id"": ""be666431-d01c-4e55-b721-c72c7c0d9c2e"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""d8ace537-9bfd-410d-a9c6-b4a0d9047972"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Pick"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Player
        m_Player = asset.FindActionMap("Player", throwIfNotFound: true);
        m_Player_Pick = m_Player.FindAction("Pick", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // Player
    private readonly InputActionMap m_Player;
    private IPlayerActions m_PlayerActionsCallbackInterface;
    private readonly InputAction m_Player_Pick;
    public struct PlayerActions
    {
        private @ObjectPick m_Wrapper;
        public PlayerActions(@ObjectPick wrapper) { m_Wrapper = wrapper; }
        public InputAction @Pick => m_Wrapper.m_Player_Pick;
        public InputActionMap Get() { return m_Wrapper.m_Player; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlayerActions set) { return set.Get(); }
        public void SetCallbacks(IPlayerActions instance)
        {
            if (m_Wrapper.m_PlayerActionsCallbackInterface != null)
            {
                @Pick.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnPick;
                @Pick.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnPick;
                @Pick.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnPick;
            }
            m_Wrapper.m_PlayerActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Pick.started += instance.OnPick;
                @Pick.performed += instance.OnPick;
                @Pick.canceled += instance.OnPick;
            }
        }
    }
    public PlayerActions @Player => new PlayerActions(this);
    public interface IPlayerActions
    {
        void OnPick(InputAction.CallbackContext context);
    }
}
