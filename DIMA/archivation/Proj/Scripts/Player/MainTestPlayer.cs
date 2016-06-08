using UnityEngine;

using SpaceBeat.Sound;
using SpaceBeat.Objects3D;
using System;

public class MainTestPlayer : MonoBehaviour
{
  //
  // Config
  //

  public Canvas LoadingScreen;

  // analyzer params
  public int sampleSize = 1024;
  public int soundFeed = 100;
  public int beatSubbands = 30;
  public double beatSensitivity = 1.5;
  public int thresholdSize = 20;
  public float thresholdMultiplier = 1.5f;

  //
  // Members
  //

  public MusicAnalyzer analyzer;
  public new AudioSource audio;

  private int lastTime;
  private int verticalDirection;

  private GameObject[] asteroids;
  private Vector3[] oldScales;

  private GameObject[] planets;
  private GameObject[] stars;
  private GameObject[] directionLights;
  private GameObject[] engineFires;

  private float peaksIntensity;


  //
  // Functions
  //

  // Use this for initialization
  void Awake()
  {
    verticalDirection = 1;
    lastTime = 0;
    peaksIntensity = 0;

    audio = GetComponent<AudioSource>();
    audio.Stop();

    analyzer = new MusicAnalyzer(
        audio.clip,
        sampleSize,
        soundFeed,
        beatSubbands,
        beatSensitivity,
        thresholdSize,
        thresholdMultiplier
      );

    // todo make loading screen or progress bar and wrap calls once per update
    //LoadingScreen.enabled = true;

    while (!analyzer.Analyze())
      ; // make fancy rotation animation

    //LoadingScreen.enabled = false;*/

    // debug
    var beats = analyzer.Beats;
    var detectedBeats = analyzer.m_soundParser.DetectedBeats;
    var thresholds = analyzer.Thresholds;

    asteroids = GameObject.FindGameObjectsWithTag("Asteroid");
    foreach (var asteroid in asteroids)
    {
      var controller = Resources.Load("Asteroid_10_L2(Clone)", typeof(RuntimeAnimatorController)) as RuntimeAnimatorController;
      asteroid.GetComponent<Animator>().runtimeAnimatorController = controller;
    }

    oldScales = new Vector3[asteroids.Length];
    for (int i = 0; i < asteroids.Length; i++)
      oldScales[i] = asteroids[i].transform.localScale;

    planets = GameObject.FindGameObjectsWithTag("Planet");
    stars = GameObject.FindGameObjectsWithTag("Star");
    directionLights = GameObject.FindGameObjectsWithTag("LightSource");
    engineFires = GameObject.FindGameObjectsWithTag("EngineFire");

    audio.Play();
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
    for (var i = lastTime; i <= time && i < peaks.Length; i++) // todo count first and then change one time
    {
      if (float.IsNaN(Math.Max(verticalDirection * (float)(peaks[i]), 10)) ||
          float.IsNaN(Math.Max((float)(peaks[i] * speedFactor * Time.deltaTime), 10)))
      {
        speedFactor = 0.0;
        Debug.Log("PLEASE I DONT HAVE TIME");
        //audio.Pause();
      }

      // move player
      transform.Translate(
          0,
          0,
          (float)speedFactor + Math.Max((float)(peaks[i] * speedFactor * Time.deltaTime), 10)
        );

      transform.Rotate(
          0,
          0,
          0
        );

      // move asteroids
      if (peaks[i] >= 10)
        foreach (var asteroid in asteroids)
          asteroid.GetComponent<Animator>().SetBool("IsBeat", true);
      else
        foreach (var asteroid in asteroids)
          asteroid.GetComponent<Animator>().SetBool("IsBeat", false);

      // change star color on beat TODO SIMILAR THING
      foreach (var light in directionLights)
      {
        if (peaks[i] <= 0)
          continue;
      
        var maxBorder = (float)peaks[i] % 256;
        var minBorder = maxBorder / 2.0f;

        //light.GetComponent<Light>().color = new Color(
        //    UnityEngine.Random.Range(minBorder, maxBorder), 
        //    UnityEngine.Random.Range(minBorder, maxBorder), 
        //    UnityEngine.Random.Range(minBorder, maxBorder)
        //  );
      }

      // emit in stars
      if (peaks[i] > 200)
        foreach (var star in stars)
          star.GetComponent<ParticleSystem>().Emit(1);

      // emit in engine
      if (peaks[i] > 50)
        foreach (var fire in engineFires)
          fire.GetComponent<ParticleSystem>().startSize = 20;
      else
        foreach (var fire in engineFires)
          fire.GetComponent<ParticleSystem>().startSize = 6;
    }

    verticalDirection = -verticalDirection;

    lastTime = time;
  }
}
