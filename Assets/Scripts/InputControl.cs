using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InputControl : MonoBehaviour
{
    public InputActionAsset inputActionAsset;

    private InputAction restartAction;
    private InputAction resetpopulationAction;

    [SerializeField] FlockingBehaviour _labelData;
    [SerializeField] TextMeshProUGUI _text;

    private void Awake()
    {
        _text.text = _labelData.MaxPopulation.ToString();
    }
    private void OnEnable()
    {
        // Enable the input action map
        var gameplayActionMap = inputActionAsset.FindActionMap("XRI RightHand Interaction");
        restartAction = gameplayActionMap.FindAction("RestartScene");
        restartAction.Enable();
        restartAction.performed += OnRestart;

        resetpopulationAction = gameplayActionMap.FindAction("SetPopulation");
        resetpopulationAction.Enable();
        resetpopulationAction.performed += OnSetPopulation;
    }

    private void OnDisable()
    {
        restartAction.performed -= OnRestart;
        restartAction.Disable();

        resetpopulationAction.performed -= OnSetPopulation;
        resetpopulationAction.Disable();
    }
    private void OnSetPopulation(InputAction.CallbackContext obj)
    {
        
        _labelData.MaxPopulation = obj.ReadValue<Vector2>().y < 0 ? _labelData.MaxPopulation - 200 : _labelData.MaxPopulation + 200;
        Debug.Log(_labelData.MaxPopulation);

        _text.text = _labelData.MaxPopulation.ToString();
           
    }
    private void OnRestart(InputAction.CallbackContext context)
    {
        // Restart the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
