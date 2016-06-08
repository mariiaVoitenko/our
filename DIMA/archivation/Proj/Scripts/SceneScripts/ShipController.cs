using UnityEngine;
using System.Collections;

public class ShipController : MonoBehaviour
{

    private float _speed;

    // Use this for initialization
    void Start()
    {
        _speed = Random.Range(5, 10);
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.position = new Vector3(gameObject.transform.position.x + _speed, gameObject.transform.position.y, gameObject.transform.position.z);
        Destroy(gameObject, 10f);
    }
}
