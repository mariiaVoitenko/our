using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingScreenAnimation : MonoBehaviour
{
  public Text LoadingText;
  public Image LoadingGear;

  private int updateAmounts;

  void Start()
  {
    updateAmounts = 0;
  }

  void Update()
  {
    if (updateAmounts % 3 == 0)
      LoadingGear.rectTransform.Rotate(0, 10, 0);

    if (updateAmounts % 10 == 0)
      LoadingText.text += ".";

    if (updateAmounts++ == 100)
      SceneManager.LoadSceneAsync("GameLevel"); // this is not how i usually write code
  }
}
