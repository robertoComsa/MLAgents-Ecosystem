using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MulakDashingTarget : MonoBehaviour
{
    [SerializeField] Transform parentTransform=null;
    // 
    public void RandomTargetPositionGenerator()
    {
        gameObject.transform.position = new Vector3(parentTransform.localPosition.x + Random.Range(-90f, 90f),
                                                    1f, 
                                                    parentTransform.localPosition.y +  Random.Range(-90f, 90f));
    }
}
