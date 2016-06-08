using UnityEngine;

public class MenuController : MonoBehaviour {

  private Animator m_Animator;
  private CanvasGroup m_CanvasGroup;

  public bool IsOpened
  {
    get { return m_Animator.GetBool("IsOpen"); }
    set { m_Animator.SetBool("IsOpen", value); }
  }

  public void Awake()
  {
    m_Animator = GetComponent<Animator>();
    m_CanvasGroup = GetComponent<CanvasGroup>();

    var rect = GetComponent<RectTransform>();
    rect.offsetMax = rect.offsetMin = new Vector2(0, 0);
  }

  public void Update()
  {
    m_CanvasGroup.blocksRaycasts = 
    m_CanvasGroup.interactable = 
      m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Open");
  }

  public void Play()
  {

  }

  public void ShowOptions()
  {

  }

  public void Quit()
  {
    Application.Quit();
  }
}
