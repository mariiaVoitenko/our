using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SpaceBeat.Sound
{
  public class SoundParser
  {
    public int HistoryBuffer = 44;

    private double[] m_FFTBytes;
    private double[] m_lastFFTBytes;

    private int[] m_detectedBeats;
    private double[] m_spectralFlux;
    private double[] m_rawBytes;
    private double[,] m_beats;

    private List<List<double>> m_energyHistoryBuffer;

    private AudioClip m_sound;
    private int m_totalSamples;
    private int m_totalBeats = 0;
    private int m_parseSampleCount = 0;

    private double m_beatSensitivity;
    private int m_beatSubbands;
    private int m_sampleSize;
    private int m_soundFeed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpaceBeat.Sound.SoundParser"/> class.
    /// </summary>
    /// <param name="sound">Main audioClip object for analyzing soundwave</param>
    /// <param name="sampleSize">Size of block in samples (can be 1024, 2048, 4096 etc.)</param>
    /// <param name="soundFeed">How many blocks analyzed in one Update's method call</param>
    /// <param name="beatSensitivity">Beat sensitivity</param>
    public SoundParser(
        AudioClip sound, 
        int sampleSize, 
        int soundFeed, 
        int beatSubbands, 
        double beatSensitivity
      )
    {
      m_sound = sound;
      m_soundFeed = soundFeed;
      m_sampleSize = sampleSize;
      m_beatSubbands = beatSubbands;
      m_beatSensitivity = beatSensitivity;

      m_totalSamples = (int)(sound.samples / m_sampleSize);
      m_rawBytes = new double[m_sampleSize];
      m_FFTBytes = new double[m_sampleSize / 2];
      m_lastFFTBytes = new double[m_sampleSize / 2];

      m_spectralFlux = new double[TotalSamples];

      m_beats = new double[TotalSamples, beatSubbands];
      m_detectedBeats = new int[TotalSamples];
      m_energyHistoryBuffer = new List<List<double>>();
    }

    /// <summary>
    /// Just section's parse mechanics with calling sound analyzing
    /// method WriteData in loop
    /// </summary>
    public bool Parse()
    {
      if (m_parseSampleCount >= TotalSamples)
        return true;

      for (int j = 0; j < m_soundFeed; j++)
      {
        if (m_parseSampleCount + j >= TotalSamples)
          break;

        WriteData(m_parseSampleCount + j);
      }

      m_parseSampleCount = (m_parseSampleCount + m_soundFeed > TotalSamples) ? TotalSamples : m_parseSampleCount + m_soundFeed;
      return false;
    }

    /// <summary>
    /// Analyzing sound function make in 5 stages:
    /// 1. Get sound data by GetData method;
    /// 2. Run Hamming window for smoothing;
    /// 3. Run FTT for calculate magnitutes;
    /// 4. Calculate beats from this algoritm: http://archive.gamedev.net/archive/reference/programming/features/beatdetection/;
    /// 5. Calculate spectral flux; 
    /// </summary>
    /// <param name="currentSample">Current sample.</param>
    private void WriteData(int currentSample)
    {
      // get raw pcm
      int startPosition = currentSample * m_sampleSize;
      float[] samples = new float[m_sampleSize * m_sound.channels];
      m_sound.GetData(samples, startPosition);

      // mono / multichannel
      if (m_sound.channels == 1)
      {
        for (int i = 0; i < samples.Length; i++)
          m_rawBytes[i] = samples[i];
      }
      else
      {
        for (int i = 0; i < samples.Length; i = i + 2)
          m_rawBytes[i / 2] = .5 * (samples[i] + samples[i + 1]);
      }

      // smooth signal to achieve better results
      RunHammingWindow();

      // transform
      RunFFT(10, false);

      m_spectralFlux[currentSample] = (currentSample < 1) ? 0 : CalculateSpectralFlux();

      // beats
      CalculateBeats(currentSample);

      // Just copy built-in arrays
      for (int i = 0; i < m_FFTBytes.Length; i++)
        m_lastFFTBytes[i] = m_FFTBytes[i];
    }

    /// <summary>
    /// Dynamic programming beat tracker.
    /// 
    /// Beats are detected in three stages, following the method of[1] _:
    ///   1. Measure onset strength
    ///   2. Estimate tempo from onset correlation
    ///   3. Pick peaks in onset strength approximately consistent with estimated
    ///      tempo
    /// 
    /// .. [1] Ellis, Daniel PW. "Beat tracking by dynamic programming."
    ///        Journal of New Music Research 36.1 (2007): 51-60.
    ///        http://labrosa.ee.columbia.edu/projects/beattrack/
    /// </summary>
    /// <param name="y">np.ndarray [shape=(n,)] or None audio time series</param>
    /// <param name="samplingRate">number > 0 [scalar] sampling rate of `y`</param>
    /// <param name="onsetEnvelope">np.ndarray [shape=(n,)] or None (optional) pre-computed onset strength envelope.</param>
    /// <param name="hopLength">int > 0 [scalar] number of audio samples between successive `onset_envelope` values</param>
    /// <param name="startBPM">float > 0 [scalar] initial guess for the tempo estimator(in beats per minute)</param>
    /// <param name="tightness">float [scalar] tightness of beat distribution around tempo</param>
    /// <param name="trim">bool [scalar] trim leading/trailing beats with weak onsets</param>
    /// <param name="bpm">float [scalar] (optional) If provided, use `bpm` as the tempo instead of estimating it from `onsets`.</param>
    /// <returns>
    /// tempo : float [scalar, non-negative]
    /// estimated global tempo(in beats per minute)
    ///
    /// beats : np.ndarray[shape = (m,)]
    ///     frame numbers of estimated beat events
    ///
    /// ..note::
    ///     If no onset strength could be detected, beat_tracker estimates 0 BPM
    ///     and returns an empty list.
    /// </returns>
    private float[] TrackBeat(
        float[] y = null,
        int samplingRate = 0,
        float[] onsetEnvelope = null,
        int hopLength = 512,
        float startBPM = 120.0f,
        int tightness = 100,
        bool trim = true,
        float bpm = -1
      )
    {
      // First, get the frame->beat strength profile if we don't already have one
      if (onsetEnvelope == null)
      {
        if (y == null)
          throw new Exception("Must provide something");

        onsetEnvelope = DetectOnsetStrenght(y, samplingRate, hopLength);
      }

      // any onsets detected?
      if (onsetEnvelope.Length == 0) // maybe incorrect
        return new float[0];

      // estimate BPM if not provided
      if (bpm == -1)
        bpm = EstimateTempo(onsetEnvelope, samplingRate, hopLength, startBPM);

      return TrackBeats(onsetEnvelope, bpm, (float)samplingRate / hopLength, tightness, trim);
    }

    private float[] TrackBeats(float[] onsetEnvelope, float bpm, float v, int tightness, bool trim)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="onsetEnvelope"></param>
    /// <param name="samplingRate"></param>
    /// <param name="hopLength"></param>
    /// <param name="startBPM"></param>
    /// <param name="stdBPM"></param>
    /// <param name="acSize"></param>
    /// <param name="duration"></param>
    /// <param name="offset"></param>
    /// <returns>estimated tempo (beats per minute)</returns>
    private float EstimateTempo(
        float[] onsetEnvelope, 
        int samplingRate, 
        int hopLength, 
        float startBPM,
        float stdBPM = 1.0f,
        float acSize = 4.0f,
        float duration = 90.0f,
        float offset = 0.0f
      )
    {
      if (startBPM <= 0)
        throw new InvalidOperationException("must be positive");

      var fft_res = (float)samplingRate / hopLength;

      // chop onsets to X[(upper_limit - duration):upper_limit]
      // or as much as will fit
      int maxcol = (int)Math.Min(onsetEnvelope.Length - 1, Math.Round((offset + duration) * fft_res));
      int mincol = (int)Math.Max(0, maxcol - Math.Round(duration * fft_res));

      // use auto-correlation out of 4 seconds (empirically set??)
      var ac_window = Math.Min(maxcol, Math.Round(acSize * fft_res));

      // compute the autocorrelation
      var x_corr = Autocorrelate(onsetEnvelope, ac_window); // maybe wrong

      // re-weight the autocorrelation by log-normal prior
      // todo numpy.arange
      //int [] arr = 
      //var bpms = 60.0f * fft_res / (Array.CreateInstance(int, )

      throw new NotImplementedException();
    }

    private object Autocorrelate(float[] onsetEnvelope, double ac_window)
    {
      throw new NotImplementedException();
    }

    private float[] DetectOnsetStrenght(float[] y, int samplingRate, int hopLength)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Method smoothing raw bytes by hamming function
    /// More: http://en.wikipedia.org/wiki/Window_function#Generalized_Hamming_windows
    /// </summary>
    private void RunHammingWindow()
    {
      for (int i = 0; i < m_rawBytes.Length; i++)
        m_rawBytes[i] = (0.54 + 0.46 * Math.Cos(2 * Math.PI * i / m_rawBytes.Length)) * m_rawBytes[i];
    }

    /// <summary>
    /// Run FFT function and then compute magnitutes of real and imagnary parts of sound.
    /// </summary>
    /// <param name="logN">Log2 of FFT length. e.g. for 512 pt FFT, logN = 9</param>
    /// <param name="inverse">If set to <c>true</c> inverse.</param>
    private void RunFFT(uint logN, bool inverse = false)
    {
      double[] re = new double[m_rawBytes.Length];
      double[] im = new double[m_rawBytes.Length];

      for (int j = 0; j < m_rawBytes.Length; j++)
      {
        re[j] = m_rawBytes[j];
        im[j] = 0;
      }

      FFT fft = new FFT();
      fft.init(logN);
      fft.run(re, im, inverse);

      for (int i = 0; i < m_FFTBytes.Length; i++)
        m_FFTBytes[i] = Math.Sqrt(re[i] * re[i] + im[i] * im[i]);
    }

    /// <summary>
    /// Calculates the spectral flux
    /// </summary>
    /// <returns>The spectral flux of current window</returns>
    private double CalculateSpectralFlux()
    {
      double diff = 0;
      double flux = 0;

      for (int i = 0; i < m_FFTBytes.Length; i++)
      {
        diff = m_FFTBytes[i] - m_lastFFTBytes[i];
        flux += Math.Max(0, diff);
      }

      return flux;
    }

    /// <summary>
    /// Beat Detector
    /// Calculates the beats based on this algoritm: http://archive.gamedev.net/archive/reference/programming/features/beatdetection/;
    /// This algorithm is used in all libs but it's not so good. Better use onset detection
    /// </summary>
    /// <param name="fftBytes">fft bytes</param>
    /// <param name="currentSample">Current analyzing sample</param>
    private void CalculateBeats(int currentSample)
    {
      double[] es = new double[m_beatSubbands];
      int detectedBeats = 0;
      int i = 0;

      // init beats on the line (zero = no beats)
      for (i = 0; i < m_beatSubbands; i++)
      {
        m_detectedBeats[currentSample] = 0;
        m_beats[currentSample, i] = es[i] = 0.0;
      }

      // divide all bytes into _beatSubbands parts
      for (i = 0; i < m_FFTBytes.Length; i++)
      {
        float subbandsDivision = m_FFTBytes.Length / (float)m_beatSubbands;
        int byteToSubband = (int)(i / subbandsDivision);
        es[byteToSubband] += m_FFTBytes[i];
      }

      if (currentSample % HistoryBuffer == 0)
      {
        if (m_energyHistoryBuffer.Count == HistoryBuffer)
        {
          for (i = 0; i < es.Length; i++)
          {
            if (es[i] > m_beatSensitivity * ComputeAverageHistoryEnergy(i))
            {
              m_beats[currentSample, i] = es[i];
              m_totalBeats++;
              detectedBeats++;
            }
          }

          m_detectedBeats[currentSample] = detectedBeats;
        }
      }

      AddHistoryEnergy(es);
    }

    /// <summary>
    /// Computes the average history energy
    /// </summary>
    /// <returns>The average history energy</returns>
    /// <param name="subband">Subband</param>
    private double ComputeAverageHistoryEnergy(int subband)
    {
      double averageEnergy = 0;

      for (int i = 0; i < m_energyHistoryBuffer.Count; i++)
        averageEnergy += m_energyHistoryBuffer[i][subband];

      return averageEnergy / m_energyHistoryBuffer.Count;
    }

    /// <summary>
    /// Method control energy history buffer size (HistoryBuffer) by queue algoritm and add new energy value into list
    /// </summary>
    /// <param name="es">Energy value</param>
    private void AddHistoryEnergy(double[] es)
    {
      if (m_energyHistoryBuffer.Count >= HistoryBuffer)
        m_energyHistoryBuffer.RemoveAt(0);

      for (int i = 0; i < es.Length; i++)
      {
        if (i == 0) m_energyHistoryBuffer.Add(new List<double>(m_beatSubbands));
        m_energyHistoryBuffer[m_energyHistoryBuffer.Count - 1].Add(es[i]);
      }
    }

    public int ParseSampleCount { get { return m_parseSampleCount; } }

    public int TotalSamples { get { return m_totalSamples; } }

    public double[] SpectralFlux { get { return m_spectralFlux; } }

    public double[,] Beats { get { return m_beats; } }

    public int[] DetectedBeats { get { return m_detectedBeats; } }

    public int TotalBeats { get { return m_totalBeats; } }
  }
}