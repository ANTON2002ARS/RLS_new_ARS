using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class O71 : AbstractBlock
{
    [Header("MultistateToggle and Action")]
    [SerializeField]
    private MultistateToggle our_phase;
    [SerializeField]
    private MultistateToggleAction _our_phase_action;

    [SerializeField]
    private MultistateToggle in_valtage;
    [SerializeField]
    private MultistateToggleAction _in_valtage_action;

    [SerializeField]
    private MultistateToggle scale;
    [SerializeField]
    private MultistateToggleAction _scale_action;

    [SerializeField]
    private string _strobModeName;

    [Header("Actions")]
    public ToggleAction _start_distation_action;
    public ToggleAction _pin_action;

    [Header("Triggers")]
    public Toggle start_distation;
    public Toggle pin;

    public void HighVoltageAction(bool state) => TriggerEventInGM( _start_distation_action, state);
    

    private void Start()
    {
        UpdateUI(false);
        our_phase.OnStateChange += HandleWorkMode;
        in_valtage.OnStateChange += HandleWorkMode;
        scale.OnStateChange += HandleWorkMode;

        start_distation.OnToggle.AddListener(HighVoltageAction);
        pin.OnToggle.AddListener(HighVoltageAction);

    }

    public override void UpdateUI(bool clearState)
    {
        if (clearState)
        {
            _our_phase_action.Reset();
            _in_valtage_action.Reset();
            _start_distation_action.Reset();
        }

        our_phase.SetStateNoEvent(_our_phase_action.CurrentState);
        in_valtage.SetStateNoEvent(_in_valtage_action.CurrentState);
        scale.SetStateNoEvent(_scale_action.CurrentState);
       
        start_distation.SetStateNoEvent(_start_distation_action.currentState);
        pin.SetStateNoEvent(_pin_action.currentState);
    }

    private void TriggerEventInGM(ToggleAction a, bool state)
    {
        a.currentState = state;
        GameManager.Instance.AddToState(a);
    }

    private void HandleWorkMode(string state)
    {
        _our_phase_action.CurrentState = state;
        _in_valtage_action.CurrentState = state;
        _scale_action.CurrentState = state;
        GameManager.Instance.AddToState(_our_phase_action);
        GameManager.Instance.AddToState(_in_valtage_action);
        GameManager.Instance.AddToState(_scale_action);
    }
}
