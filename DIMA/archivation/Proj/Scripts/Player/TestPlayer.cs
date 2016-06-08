using UnityEngine;

using SpaceBeat.Sound;
using SpaceBeat.Objects3D;
using System;

public class TestPlayer : MonoBehaviour
{
  //
  // Config
  //

  //
  // Members
  //

  private MusicAnalyzer analyzer;
  private new AudioSource audio;
  
  private int sampleSize;

  private int lastTime;
  private int verticalDirection;

  //
  // Functions
  //

  // Use this for initialization
  void Start()
  {
    verticalDirection = 1;
    lastTime = 0;

    var mainObject = GameObject.Find("MainObject");
    var script = mainObject.GetComponent<MainTestPlayer>();

    analyzer = script.analyzer;
    audio = script.audio;
    sampleSize = script.sampleSize;
  }

  // Update is called once per frame
  void Update()
  {
    var time = audio.timeSamples / sampleSize;

    //var beats = analyzer.Beats;
    var thresholds = analyzer.Thresholds;
    var peaks = analyzer.Peaks;
    var speedFactor = double.IsNaN(analyzer.SpeedFactor) ? 0.0 : analyzer.SpeedFactor;

    var detectedBeats = analyzer.m_soundParser.DetectedBeats;
    for (var i = lastTime; i <= time; i++) // todo count first and then change one time
    {
      if (float.IsNaN(Math.Max(verticalDirection * (float)(peaks[i]), 10)) ||
          float.IsNaN(Math.Max((float)(peaks[i] * speedFactor * Time.deltaTime), 10)))
      {
        Debug.Log("WAHTSHTE FUCKL");
        audio.Pause();
      }

      transform.Translate(
          0,
          Math.Max(verticalDirection * (float)(peaks[i]), 10),
          Math.Max((float)(peaks[i] * speedFactor * Time.deltaTime), 10)
        );
    }

    verticalDirection = -verticalDirection;


    lastTime = time;
  }
}
