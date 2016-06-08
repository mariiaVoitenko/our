using System;
using System.Linq;
using UnityEngine;

namespace SpaceBeat.Sound
{
  public class MusicAnalyzer
  {
    public SoundParser m_soundParser;

    private float    m_thresholdMultiplier;
    private double[] m_threshold;
    private double[] m_peaks;
    private double   m_sumOfFluxThresholds;
    private int      m_thresholdSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpaceBeat.Sound.MusicAnalyzer"/> class.
    /// MusicAnalyzer creates array of 3d track heights (0..1) analyzing sound parameters. 
    /// </summary>
    /// <param name="sound">Main audioClip object for analyzing soundwave</param>
    /// <param name="sampleSize">Size of block in samples (can be 1024, 2048, 4096 etc.)</param>
    /// <param name="soundFeed">How many blocks analyzed in one Update's method call</param>
    /// <param name="beatSubbands">"How many spectrum divisions are made</param>
    /// <param name="beatSensitivity">Beat sensitivity of Beat Detector</param>
    /// <param name="thresholdSize">Threshold size</param>
    /// <param name="thresholdMultiplier">Threshold multiplier</param>
    public MusicAnalyzer(
        AudioClip sound,
        int sampleSize = 1024,
        int soundFeed = 40,
        int beatSubbands = 3,
        double beatSensitivity = 1.5,
        int thresholdSize = -1, // TODO TWEAK THIS
        float thresholdMultiplier = 1.5f
      )
    {
      m_thresholdMultiplier = thresholdMultiplier;


      m_thresholdSize = (thresholdSize < 0) ? (int)(3 * sound.length) : thresholdSize; // FUCKS SAKE NO

      m_soundParser = new SoundParser(sound, sampleSize, soundFeed, beatSubbands, beatSensitivity);
      m_threshold = new double[m_soundParser.TotalSamples];
      m_peaks = new double[m_soundParser.TotalSamples];
    }

    /// <summary>
    /// Analyze method calls when MonoBehaviour calls Update method.
    /// It call main Parse method of the m_soundParser object. After parse
    /// MusicAnalyzer calculate FluxThresholds, smooth it by Kalman's filter
    /// and convert in 1..0 representation.
    /// </summary>
    public bool Analyze()
    {
      if (m_soundParser.Parse())
      {
        CalculateFluxThresholds();
        CalculateKalmanFilter();
        DetectPeaks();
        ConvertToPercents();
        CalculateSumOfThresholds();

        return true;
      }

      Debug.Log("Parsed " + 100 * Math.Round(m_soundParser.ParseSampleCount / (float)m_soundParser.TotalSamples, 2) + "% of sound");

      return false;
    }

    /// <summary>
    /// Calculates the flux thresholds.
    /// </summary>
    private void CalculateFluxThresholds()
    {
      for (int i = 0; i < m_soundParser.TotalSamples; i++)
      {
        int start = Math.Max(0, i - m_thresholdSize / 2);
        int end   = Math.Min(m_soundParser.SpectralFlux.Length - 1, i + m_thresholdSize / 2);

        double mean = 0;
        for (int j = start; j <= end; j++)
          mean += m_soundParser.SpectralFlux[j];

        mean /= (end - start);

        m_threshold[i] = mean * m_thresholdMultiplier;
      }
    }

    /// <summary>
    /// Alternate way to detect peaks.
    /// </summary>
    private void DetectPeaks()
    {
      double[] prunnedSpectralFlux = new double[m_threshold.Length];

      for (int i = 0; i < m_threshold.Length; i++)
      {
        prunnedSpectralFlux[i] = Math.Max(0, m_soundParser.SpectralFlux[i] - m_threshold[i]);
      }

      for (int i = 0; i < prunnedSpectralFlux.Length - 1; i++)
      {
        if (prunnedSpectralFlux[i] > prunnedSpectralFlux[i + 1])
          m_peaks[i] = prunnedSpectralFlux[i];
        else
          m_peaks[i] = 0;
      }
    }

    /// <summary>
    /// Kalman's filter for smoothing xy function. In this case filter will smooth flux thresholds function.
    /// More: http://en.wikipedia.org/wiki/Kalman_filter
    /// </summary>
    /// <param name="q">Measurement noise</param>
    /// <param name="r">Environment noise</param>
    /// <param name="f">Factor of real value to previous real value</param>
    /// <param name="h">Factor of measured value to real value</param>
    private void CalculateKalmanFilter(double q = .35, double r = 35, double f = 1, double h = 1)
    {
      double state = m_threshold[0];
      double covariance = .1;

      for (int i = 0; i < m_threshold.Length; i++)
      {
        double x0 = f * state;
        double p0 = f * covariance * f + q;
        double k = h * p0 / (h * p0 * h + r);
        state = x0 + k * (m_threshold[i] - h * x0);
        covariance = (1 - k * h) * p0;
        m_threshold[i] = state;
      }
    }

    /// <summary>
    /// Convert all flux thresholds values into 0..1 representation.
    /// </summary>
    private void ConvertToPercents()
    {
      double maxFlux = m_threshold.Max();

      for (int i = 0; i < m_threshold.Length; i++)
        m_threshold[i] = m_threshold[i] / maxFlux;
    }

    private void CalculateSumOfThresholds()
    {
      m_sumOfFluxThresholds = 0;

      for (int i = 0; i < m_threshold.Length; i++)
        m_sumOfFluxThresholds += m_threshold[i];
    }

    public double[] Thresholds { get { return m_threshold; } }
    public double[] Peaks { get { return m_peaks; } }
    public double[,] Beats { get { return m_soundParser.Beats; } }
    public double SpeedFactor { get { return m_threshold.Length / m_sumOfFluxThresholds; } }
  }
}
