using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MulakDashingTarget : MonoBehaviour
{
    [SerializeField] Transform parentTransform=null;
    // 
    public void RandomTargetPositionGenerator()
    {
        gameObject.transform.position = new Vector3(parentTransform.localPosition.x + Random.Range(-180f, 180f),
                                                    1f, 
                                                    parentTransform.localPosition.y +  Random.Range(-180f, 180f));
    }

    private void Awake()
    {
        RandomTargetPositionGenerator();
    }

    private void Start()
    {
        StartCoroutine(ResetPosition());
    }

    IEnumerator ResetPosition()
    {
        yield return new WaitForSeconds(800f);
        RandomTargetPositionGenerator();
        StartCoroutine(ResetPosition());
    }
}
