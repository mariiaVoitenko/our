using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour {

  public void OpenMainMenu()
  {
    SceneManager.LoadScene("MainMenu");
  }

  public void StartGame()
  {
    SceneManager.LoadScene("LoadingScreen");
  }

  public void ShowOptions()
  {
    SceneManager.LoadScene("Settings");
  }

  public void Quit()
  {
    Application.Quit();
  }
}
