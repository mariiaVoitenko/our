using UnityEngine;
using System.Collections;

public class SunGrower : MonoBehaviour
{
  private int m_LastUpdate;

  // Use this for initialization
  void Start()
  {
    m_LastUpdate = 0;
  }

  // Update is called once per frame
  void FixedUpdate()
  {
    m_LastUpdate++;
    
    transform.localScale += new Vector3(0.001f, 0.001f, 0.001f);
  }
}
