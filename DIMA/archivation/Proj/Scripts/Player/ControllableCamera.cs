﻿/*
*
* Used code from GhostFreeRoamCamera by Goosey Gamez
*
*/

using UnityEngine;

public class ControllableCamera : MonoBehaviour
{

  public float initialSpeed = 10f;
  public float increaseSpeed = 1.25f;

  public bool allowMovement = true;
  public bool allowRotation = true;

  public KeyCode forwardButton = KeyCode.W;
  public KeyCode backwardButton = KeyCode.S;
  public KeyCode rightButton = KeyCode.D;
  public KeyCode leftButton = KeyCode.A;

  public float cursorSensitivity = 0.025f;
  public bool cursorToggleAllowed = true;
  public KeyCode cursorToggleButton = KeyCode.Escape;

  private float currentSpeed = 0f;
  private bool moving = false;
  private bool togglePressed = false;
  private float eulerAngle = 359f;

  private void OnEnable()
  {
    if (cursorToggleAllowed)
    {
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;
    }
  }

  private void Update()
  {
    if (allowMovement)
    {
      bool lastMoving = moving;
      Vector3 deltaPosition = Vector3.zero;

      if (moving)
        currentSpeed += increaseSpeed * Time.deltaTime;

      moving = false;

      CheckMove(forwardButton, ref deltaPosition, transform.forward);
      CheckMove(backwardButton, ref deltaPosition, -transform.forward);
      CheckMove(rightButton, ref deltaPosition, transform.right);
      CheckMove(leftButton, ref deltaPosition, -transform.right);

      if (moving)
      {
        if (moving != lastMoving)
          currentSpeed = initialSpeed;

        transform.position += deltaPosition * currentSpeed * Time.deltaTime;
      }
      else currentSpeed = 0f;
    }

    if (allowRotation)
    {
      Vector3 eulerAngles = transform.eulerAngles;
      eulerAngles.x += -Input.GetAxis("Mouse Y") * eulerAngle * cursorSensitivity;
      eulerAngles.y += Input.GetAxis("Mouse X") * eulerAngle * cursorSensitivity;
      transform.eulerAngles = eulerAngles;
    }

    if (cursorToggleAllowed)
    {
      if (Input.GetKey(cursorToggleButton))
      {
        if (!togglePressed)
        {
          togglePressed = true;
          Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked; // swap between lock unlock
          Cursor.visible = !Cursor.visible;
        }
      }
      else
        togglePressed = false;
    }
    else
    {
      togglePressed = false;
      Cursor.visible = false;
    }
  }

  private void CheckMove(KeyCode keyCode, ref Vector3 deltaPosition, Vector3 directionVector)
  {
    if (Input.GetKey(keyCode))
    {
      moving = true;
      deltaPosition += directionVector;
    }
  }
}
