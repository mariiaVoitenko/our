using SpaceBeat.Objects3D;
using UnityEngine;

namespace SpaceBeat.Sound
{
  public class Main : MonoBehaviour
  {
    //
    // Config
    //

    public double RadiansToDegrees = 57.29577951308232;
    public double CameraRotationEasing = 0.08;
    public double PlayerMovingEasing = 0.1;
    public float thresholdMultiplier = 1.5f;
    public int thresholdSize = -1;
    public double beatSensitivity = 1.5;
    public int beatSubbands = 3;
    public int sampleSize = 1024;
    public int soundFeed = 100;
    public int trackWidth = 1000;
    public int trackHeight = 100;
    public int trackDepth = 10000;
    public int cameraDistanceY = 300;
    public int cameraDistanceZ = 300;

    //
    // Members
    //

    private MusicAnalyzer analyzer;
    private new AudioSource audio;
    private double targetPlayerZ = 0.0;
    private double playerZ = 0.0;
    private int lastSample = 0;
    private bool isAnalyzed = false;

    //
    // Functions
    //

    void Start()
    {
      audio = GetComponent<Camera>().GetComponent<AudioSource>();
      analyzer = new MusicAnalyzer(
          GetComponent<Camera>().GetComponent<AudioSource>().clip,
          sampleSize,
          soundFeed,
          beatSubbands,
          beatSensitivity,
          thresholdSize,
          thresholdMultiplier
        );
    }

    void Update()
    {
      if (isAnalyzed)
      {
        if (!audio.isPlaying)
          audio.Play();

        UpdateTarget();
        playerZ += PlayerMovingEasing * (targetPlayerZ - playerZ);
        UpdateCamera();

        return;
      }

      if (analyzer.Analyze())
      {
        Track track = new Track(trackWidth, trackHeight, trackDepth, Color.black, analyzer.Thresholds);
        GetComponent<Camera>().transform.position = new Vector3(-trackWidth / 2, (float)(analyzer.Thresholds[0] * trackDepth + 100), 0);

        isAnalyzed = true;
      }
    }

    void UpdateCamera()
    {
      double diff;
      double d = -((playerZ + cameraDistanceZ) / trackHeight);
      int c = (int)d;

      if (c > 0)
      {
        diff = analyzer.Thresholds[c + 1] - analyzer.Thresholds[c];

        GetComponent<Camera>().transform.position = new Vector3(
            -trackWidth / 2,
            (float)((analyzer.Thresholds[c] + (d - c) * diff) * trackDepth + cameraDistanceY),
            -(float)(playerZ + cameraDistanceZ)
           );
      }
    }

    void UpdateTarget()
    {
      int c = (int)(audio.timeSamples / sampleSize);

      if (c > lastSample && lastSample < analyzer.Thresholds.Length)
      {
        for (int i = lastSample + 1; i <= c; i++)
          targetPlayerZ += -analyzer.Thresholds[i] * trackHeight * analyzer.SpeedFactor;
        lastSample = c;
      }
    }

  }
}
