using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PF71 : AbstractBlock
{
    [Header("Actions")]
    public ToggleAction _diferences_action;
    public ToggleAction _fp_action;
    public ToggleAction _anode_action;
   

    [Header("Triggers")]
    public Toggle diferences;
    public Toggle fp;
    public Toggle anode;
    

    public void HighVoltageAction(bool state) => TriggerEventInGM(_diferences_action, state);
    public void PowerAction(bool state) => TriggerEventInGM(_fp_action, state);
    public void RotationAction(bool state) => TriggerEventInGM(_anode_action, state);
    

    private void Start()
    {
        UpdateUI(false);

        diferences.OnToggle.AddListener(HighVoltageAction);
        fp.OnToggle.AddListener(PowerAction);
        anode.OnToggle.AddListener(RotationAction);
    }

    public override void UpdateUI(bool clearState)
    {
        diferences.SetStateNoEvent(_diferences_action.currentState);
        fp.SetStateNoEvent(_fp_action.currentState);
        anode.SetStateNoEvent(_anode_action.currentState);
        
    }

    private void TriggerEventInGM(ToggleAction a, bool state)
    {
        a.currentState = state;
        GameManager.Instance.AddToState(a);
    }


    /*private void HandleWorkMode(string state)
    {
        _workModeAction.CurrentState = state;
        _switch_canals_action.CurrentState = state;
        
        GameManager.Instance.AddToState(_workModeAction);
        GameManager.Instance.AddToState(_switch_canals_action);
    }*/
}
