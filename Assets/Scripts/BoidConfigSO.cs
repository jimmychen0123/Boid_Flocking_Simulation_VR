using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoidConfigSO", menuName = "ScriptableObjects/BoidConfigSO", order = 1)]
public class BoidConfigSO : ScriptableObject
{
    public int MaxPopulation = 5000;
}
